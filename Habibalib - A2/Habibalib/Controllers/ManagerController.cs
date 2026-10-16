using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;
using Habibalib.Services;
using Habibalib.Models.Integrations;

namespace Habibalib.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly AppDbContext db;
        private readonly ReportService reports;
        private readonly ReportExportService exports;

        public ManagerController(AppDbContext context, ReportService reports, ReportExportService exports)
        {
            db = context;
            this.reports = reports;
            this.exports = exports;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Reports([FromQuery] ReportFilter filter)
        {
            if (!ModelState.IsValid) return View(new ReportData { Filter = filter });
            return View(reports.Build(filter));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Export([FromQuery] ReportFilter filter, string format = "csv")
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (format != "csv" && format != "pdf") return BadRequest("Format must be csv or pdf.");
            var report = reports.Build(filter);
            var filename = $"habibalib-{filter.Kind}-{report.GeneratedUtc:yyyyMMdd-HHmmss}.{format}";
            return format == "csv"
                ? File(exports.Csv(report), "text/csv; charset=utf-8", filename)
                : File(exports.Pdf(report), "application/pdf", filename);
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
