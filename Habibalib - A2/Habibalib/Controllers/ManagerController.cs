using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;

namespace Habibalib.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly AppDbContext db;

        public ManagerController(AppDbContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalBooks = db.Books.Count();
            ViewBag.TotalMusic = db.Music.Count();
            ViewBag.TotalToys = db.Toys.Count();

            ViewBag.AvailableCount = db.Books.Count(b => b.Status == "available" && !b.IsInTransit)
                + db.Music.Count(m => m.Status == "available")
                + db.Toys.Count(t => t.Status == "available");

            ViewBag.BorrowedCount = db.Books.Count(b => b.Status == "borrowed")
                + db.Music.Count(m => m.Status == "borrowed")
                + db.Toys.Count(t => t.Status == "borrowed");

            ViewBag.DamagedCount = db.Books.Count(b => b.Status == "damaged")
                + db.Music.Count(m => m.Status == "damaged")
                + db.Toys.Count(t => t.Status == "damaged");

            ViewBag.DestroyCount = db.Books.Count(b => b.Status == "destroy")
                + db.Music.Count(m => m.Status == "destroy")
                + db.Toys.Count(t => t.Status == "destroy");

            ViewBag.TotalBorrows = db.BorrowTransactions.Count();
            ViewBag.ActiveBorrows = db.BorrowTransactions.Count(t => t.ReturnDate == null);

            // sqlite cant do Sum() on decimal directly in the database, so we
            // grab the rows first then add them up here in normal C# code
            var allTrans = db.BorrowTransactions.ToList();
            decimal totalFine = 0;
            decimal unpaidFine = 0;
            foreach (var t in allTrans)
            {
                totalFine = totalFine + t.Fine;
                if (!t.Paid)
                {
                    unpaidFine = unpaidFine + t.Fine;
                }
            }
            ViewBag.TotalFine = totalFine;
            ViewBag.UnpaidFine = unpaidFine;

            return View();
        }
    }
}
