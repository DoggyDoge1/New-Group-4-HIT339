using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Habibalib.Data;
using Habibalib.Models;

namespace Habibalib.Controllers
{
    // Public self-service kiosk. No cookies: the session lives in server memory and
    // its random token is carried in the URL / hidden form fields, so nothing
    // persists in the shared browser. Sessions expire after 60 seconds of inactivity.
    [AllowAnonymous]
    [ServiceFilter(typeof(Habibalib.Exceptions.KioskExceptionFilter))]
    [IgnoreAntiforgeryToken] // antiforgery relies on cookies; the unguessable session token is used instead
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class KioskController : Controller
    {
        public const int IdleSeconds = 60;
        private static readonly TimeSpan Idle = TimeSpan.FromSeconds(IdleSeconds);

        private readonly AppDbContext db;
        private readonly IMemoryCache cache;
        private readonly IConfiguration configuration;

        public KioskController(AppDbContext context, IMemoryCache memoryCache, IConfiguration configuration)
        {
            db = context;
            cache = memoryCache;
            this.configuration = configuration;
        }

        private class KioskSession
        {
            public int BorrowerId { get; set; }
            public System.Collections.Generic.List<KioskSessionItem> Items { get; } = new();
        }

        private KioskSession Sess(string t) =>
            cache.TryGetValue(Key(t ?? ""), out KioskSession s) ? s : null;

        private static string Key(string t) => "kiosk:" + t;

        private Borrower GetPatron(string t)
        {
            if (string.IsNullOrEmpty(t) || !cache.TryGetValue(Key(t), out KioskSession s)) return null;
            cache.Set(Key(t), s, Idle); // sliding refresh on every interaction
            return db.Borrowers.FirstOrDefault(b => b.Id == s.BorrowerId);
        }

        [HttpGet]
        public IActionResult Index(string msg = null)
        {
            ViewBag.Error = msg;
            return View("KioskIndex");
        }

        // 404s under /Kiosk are routed here (see Program.cs); also viewable directly.
        [HttpGet]
        public IActionResult Error(int code = 500)
        {
            Response.StatusCode = code;
            return View("KioskError", KioskErrorModel.For(code));
        }

        [HttpPost]
        public IActionResult Start(string cardNumber)
        {
            // scanners send the code then Enter; strip whitespace they may add
            var code = (cardNumber ?? "").Trim();
            if (!int.TryParse(code, out int id) || db.Borrowers.FirstOrDefault(b => b.Id == id) == null)
            {
                ViewBag.Error = "Card not recognised. Please try again or ask staff for help.";
                return View("KioskIndex");
            }

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            cache.Set(Key(token), new KioskSession { BorrowerId = id }, Idle);
            return RedirectToAction("Menu", new { t = token });
        }

        [HttpGet]
        public IActionResult Menu(string t, string mode = "borrow", string msg = null, bool ok = false)
        {
            var patron = GetPatron(t);
            if (patron == null) return RedirectToAction("Index", new { msg = "Your session timed out." });

            return View("KioskMenu", BuildModel(t, patron, mode, msg, ok));
        }

        [HttpPost]
        public IActionResult Scan(string t, string mode, string libraryCode)
        {
            var patron = GetPatron(t);
            if (patron == null) return RedirectToAction("Index", new { msg = "Your session timed out." });

            var code = (libraryCode ?? "").Trim();
            if (code.Length == 0)
                return RedirectToAction("Menu", new { t, mode, msg = "Please scan or enter an item code." });

            string result;
            bool ok;
            if (mode == "return") ok = Return(patron, code, out result);
            else ok = Borrow(patron, code, Sess(t), out result);

            return RedirectToAction("Menu", new { t, mode, msg = result, ok });
        }

        // undo a checkout made during this session
        [HttpPost]
        public IActionResult Remove(string t, int id)
        {
            var patron = GetPatron(t);
            if (patron == null) return RedirectToAction("Index", new { msg = "Your session timed out." });

            var s = Sess(t);
            var entry = s.Items.FirstOrDefault(i => i.TransactionId == id);
            if (entry == null) return RedirectToAction("Menu", new { t });

            var trans = db.BorrowTransactions.FirstOrDefault(x => x.Id == id && x.BorrowerId == patron.Id && x.ReturnDate == null);
            var item = FindItem(entry.Barcode, out _);
            if (trans != null) db.BorrowTransactions.Remove(trans);
            if (item != null) item.Status = "available";
            db.SaveChanges();
            s.Items.Remove(entry);

            return RedirectToAction("Menu", new { t, msg = "Removed \"" + entry.Name + "\".", ok = true });
        }

        [HttpPost]
        public IActionResult Finish(string t, bool receipt = false)
        {
            if (receipt)
            {
                var patron = GetPatron(t);
                var s = Sess(t);
                if (patron != null && s != null)
                {
                    var model = new KioskReceiptModel
                    {
                        PatronName = patron.Name,
                        Items = s.Items.ToList(),
                        Printed = DateTime.Now
                    };
                    cache.Remove(Key(t));
                    return View("KioskReceipt", model);
                }
            }
            if (!string.IsNullOrEmpty(t)) cache.Remove(Key(t));
            return RedirectToAction("Index");
        }

        private KioskMenuModel BuildModel(string t, Borrower patron, string mode, string msg, bool ok)
        {
            var loans = db.BorrowTransactions
                .Where(x => x.BorrowerId == patron.Id && x.ReturnDate == null)
                .OrderBy(x => x.DueDate).ToList();
            var unpaid = db.BorrowTransactions
                .Where(x => x.BorrowerId == patron.Id && !x.Paid).ToList();

            // recorded fines on returned items, plus fines accruing on overdue loans ($10/day)
            decimal fines = unpaid.Where(x => x.ReturnDate != null).Sum(x => x.Fine)
                + loans.Where(x => x.DueDate < DateTime.Now).Sum(x => (DateTime.Now - x.DueDate).Days * 10m);

            var name = (patron.Name ?? "").Trim();
            return new KioskMenuModel
            {
                Token = t,
                PatronName = name,
                SessionItems = Sess(t)?.Items.ToList() ?? new System.Collections.Generic.List<KioskSessionItem>(),
                AccountLoans = loans.Where(l => !(Sess(t)?.Items.Any(i => i.TransactionId == l.Id) ?? false)).ToList(),
                OutstandingFines = fines,
                Mode = mode == "return" ? "return" : "borrow",
                Message = msg,
                Success = ok,
                IdleSeconds = IdleSeconds,
                Loans = loans
            };
        }

        private Item FindItem(string code, out string type)
        {
            type = "";
            Item found = null;
            var book = db.Books.FirstOrDefault(b => b.LibraryCode == code);
            if (book != null) { found = book; type = "Book"; }
            var music = db.Music.FirstOrDefault(m => m.LibraryCode == code);
            if (music != null) { found = music; type = "Music"; }
            var toy = db.Toys.FirstOrDefault(x => x.LibraryCode == code);
            if (toy != null) { found = toy; type = "Toy"; }
            return found;
        }

        private bool Borrow(Borrower patron, string code, KioskSession s, out string message)
        {
            var item = FindItem(code, out string type);
            if (item == null) { message = "No item found with that code."; return false; }
            if (item is Book book
                && (book.CurrentBranchId != configuration.GetValue<int>("BranchSettings:LocationBranchId", 1)
                    || book.IsInTransit))
            {
                message = "Sorry, \"" + item.Name + "\" is not available at this branch.";
                return false;
            }
            if (item.Status != "available") { message = "Sorry, \"" + item.Name + "\" is not available."; return false; }

            item.Status = "borrowed";
            var trans = new BorrowTransaction
            {
                ItemType = type,
                ItemId = item.Id,
                ItemName = item.Name,
                BorrowerId = patron.Id,
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                Fine = 0,
                Paid = false
            };
            db.BorrowTransactions.Add(trans);
            db.SaveChanges();

            s?.Items.Add(new KioskSessionItem { TransactionId = trans.Id, Name = item.Name, Barcode = item.LibraryCode, DueDate = trans.DueDate });

            message = "Checked Out! \"" + item.Name + "\" is due back in 14 days.";
            return true;
        }

        private bool Return(Borrower patron, string code, out string message)
        {
            var item = FindItem(code, out string type);
            if (item == null) { message = "No item found with that code."; return false; }

            var trans = db.BorrowTransactions.FirstOrDefault(x =>
                x.ItemType == type && x.ItemId == item.Id && x.BorrowerId == patron.Id && x.ReturnDate == null);
            if (trans == null) { message = "\"" + item.Name + "\" is not on loan to you."; return false; }

            trans.ReturnDate = DateTime.Now;
            string fineNote = "";
            if (trans.ReturnDate > trans.DueDate)
            {
                int lateDays = (trans.ReturnDate.Value - trans.DueDate).Days;
                trans.Fine = lateDays * 10;
                if (trans.Fine > 0) fineNote = " Late fine: $" + trans.Fine + ". Please see the desk.";
            }
            item.Status = "available";
            if (item is Book returnedBook)
            {
                returnedBook.CurrentBranchId = configuration.GetValue<int>("BranchSettings:LocationBranchId", 1);
            }
            db.SaveChanges();

            message = "Returned \"" + item.Name + "\"." + fineNote;
            return true;
        }
    }

    public class KioskMenuModel
    {
        public string Token { get; set; }
        public string PatronName { get; set; }
        public string Mode { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public int IdleSeconds { get; set; }
        public decimal OutstandingFines { get; set; }
        public System.Collections.Generic.List<BorrowTransaction> Loans { get; set; }
        public System.Collections.Generic.List<KioskSessionItem> SessionItems { get; set; }
        // loans from before this session (excludes items scanned now)
        public System.Collections.Generic.List<BorrowTransaction> AccountLoans { get; set; }
    }

    public class KioskSessionItem
    {
        public int TransactionId { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class KioskErrorModel
    {
        public int Code { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        public static KioskErrorModel For(int code) => code == 404
            ? new KioskErrorModel { Code = 404, Title = "Page not found", Message = "That kiosk page doesn't exist. Please return to the start screen." }
            : new KioskErrorModel { Code = code, Title = "Something went wrong", Message = "Sorry, the kiosk couldn't complete that. Please try again or ask a staff member for help." };
    }

    public class KioskReceiptModel
    {
        public string PatronName { get; set; }
        public System.Collections.Generic.List<KioskSessionItem> Items { get; set; }
        public DateTime Printed { get; set; }
    }
}
