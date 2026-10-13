using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;
using Habibalib.Models;

namespace Habibalib.Controllers
{
    [Authorize(Roles = "Reception")]
    public class BorrowerController : Controller
    {
        private readonly AppDbContext db;

        public BorrowerController(AppDbContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            var list = db.Borrowers.ToList();
            return View(list);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Borrower borrower)
        {
            db.Borrowers.Add(borrower);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var borrower = db.Borrowers.FirstOrDefault(b => b.Id == id);
            if (borrower == null) return NotFound();
            return View(borrower);
        }

        [HttpPost]
        public IActionResult Edit(Borrower borrower)
        {
            db.Borrowers.Update(borrower);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var borrower = db.Borrowers.FirstOrDefault(b => b.Id == id);
            if (borrower != null)
            {
                db.Borrowers.Remove(borrower);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
