using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;
using Habibalib.Models;
using Habibalib.Services;

namespace Habibalib.Controllers
{
    // lets a public patron join the waitlist for a borrowed/damaged item,
    // and lets reception staff manage that waitlist
    public class ReservationController : Controller
    {
        private readonly AppDbContext db;

        public ReservationController(AppDbContext context)
        {
            db = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Reserve(string type, int id)
        {
            var item = FindItem(type, id);
            if (item == null) return NotFound();

            ViewBag.ItemType = type;
            ViewBag.ItemId = id;
            ViewBag.ItemName = item.Name;
            ViewBag.ItemStatus = item.Status;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Reserve(string type, int id, string name, string email)
        {
            var item = FindItem(type, id);
            if (item == null) return NotFound();

            ViewBag.ItemType = type;
            ViewBag.ItemId = id;
            ViewBag.ItemName = item.Name;
            ViewBag.ItemStatus = item.Status;

            if (item.Status != "borrowed" && item.Status != "damaged")
            {
                ViewBag.Error = "this item is available right now, you can just borrow it";
                return View();
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "please enter your name and email";
                return View();
            }

            var borrower = db.Borrowers.FirstOrDefault(b => b.Email == email);
            if (borrower == null)
            {
                borrower = new Borrower { Name = name, Email = email };
                db.Borrowers.Add(borrower);
                db.SaveChanges();
            }

            bool already = db.Reservations.Any(r =>
                r.ItemType == type && r.ItemId == id && r.BorrowerId == borrower.Id && r.Status == "waiting");
            if (already)
            {
                ViewBag.Error = "you already have a hold on this item";
                return View();
            }

            var res = new Reservation
            {
                ItemType = type,
                ItemId = id,
                ItemName = item.Name,
                BorrowerId = borrower.Id,
                BorrowerName = borrower.Name,
                ReservedDate = DateTime.Now,
                Status = "waiting"
            };
            db.Reservations.Add(res);
            db.SaveChanges();

            int position = db.Reservations.Count(r =>
                r.ItemType == type && r.ItemId == id && r.Status == "waiting" && r.ReservedDate <= res.ReservedDate);

            NotificationHelper.Send(db, borrower.Id, borrower.Name, "Email", "Reservation",
                "You're #" + position + " in line for \"" + item.Name + "\". We will email you when it's ready.");

            ViewBag.Success = "You are #" + position + " in line for \"" + item.Name + "\".";
            return View();
        }

        [Authorize(Roles = "Reception")]
        public IActionResult Index()
        {
            NotificationHelper.RunDueDateSweep(db);

            var list = db.Reservations
                .Where(r => r.Status == "waiting" || r.Status == "notified")
                .OrderBy(r => r.ItemType).ThenBy(r => r.ItemId).ThenBy(r => r.ReservedDate)
                .ToList();
            return View(list);
        }

        [Authorize(Roles = "Reception")]
        [HttpPost]
        public IActionResult Cancel(int id)
        {
            var res = db.Reservations.FirstOrDefault(r => r.Id == id);
            if (res != null)
            {
                res.Status = "cancelled";
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Reception")]
        [HttpPost]
        public IActionResult Fulfil(int id)
        {
            var res = db.Reservations.FirstOrDefault(r => r.Id == id);
            if (res != null)
            {
                res.Status = "fulfilled";
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        private Item FindItem(string type, int id)
        {
            if (type == "Book") return db.Books.FirstOrDefault(x => x.Id == id);
            if (type == "Music") return db.Music.FirstOrDefault(x => x.Id == id);
            if (type == "Toy") return db.Toys.FirstOrDefault(x => x.Id == id);
            return null;
        }
    }
}
