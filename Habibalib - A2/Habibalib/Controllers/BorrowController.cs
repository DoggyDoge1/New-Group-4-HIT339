using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;
using Habibalib.Models;

namespace Habibalib.Controllers
{
    [Authorize(Roles = "Reception")]
    public class BorrowController : Controller
    {
        private readonly AppDbContext db;

        public BorrowController(AppDbContext context)
        {
            db = context;
        }

        public IActionResult BorrowItem()
        {
            ViewBag.Borrowers = db.Borrowers.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult BorrowItem(string libraryCode, int borrowerId)
        {
            Item found = null;
            string type = "";

            var book = db.Books.FirstOrDefault(b => b.LibraryCode == libraryCode);
            if (book != null) { found = book; type = "Book"; }

            var music = db.Music.FirstOrDefault(m => m.LibraryCode == libraryCode);
            if (music != null) { found = music; type = "Music"; }

            var toy = db.Toys.FirstOrDefault(t => t.LibraryCode == libraryCode);
            if (toy != null) { found = toy; type = "Toy"; }

            if (found == null)
            {
                ViewBag.Error = "no item found with that library code";
                ViewBag.Borrowers = db.Borrowers.ToList();
                return View();
            }

            if (found.Status != "available")
            {
                ViewBag.Error = "this item is not available right now";
                ViewBag.Borrowers = db.Borrowers.ToList();
                return View();
            }

            found.Status = "borrowed";

            if (type == "Book") db.Books.Update(book);
            if (type == "Music") db.Music.Update(music);
            if (type == "Toy") db.Toys.Update(toy);

            var trans = new BorrowTransaction
            {
                ItemType = type,
                ItemId = found.Id,
                ItemName = found.Name,
                BorrowerId = borrowerId,
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                Fine = 0,
                Paid = false
            };

            db.BorrowTransactions.Add(trans);
            db.SaveChanges();

            return RedirectToAction("Index", "Borrower");
        }

        public IActionResult ReturnItem()
        {
            var list = db.BorrowTransactions.Where(t => t.ReturnDate == null).ToList();
            return View(list);
        }

        [HttpPost]
        public IActionResult ReturnItem(int id)
        {
            var trans = db.BorrowTransactions.FirstOrDefault(t => t.Id == id);
            if (trans == null) return NotFound();

            trans.ReturnDate = DateTime.Now;

            if (trans.ReturnDate > trans.DueDate)
            {
                int lateDays = (trans.ReturnDate.Value - trans.DueDate).Days;
                trans.Fine = lateDays * 10;
            }

            if (trans.ItemType == "Book")
            {
                var book = db.Books.FirstOrDefault(b => b.Id == trans.ItemId);
                if (book != null) { book.Status = "available"; db.Books.Update(book); }
            }
            if (trans.ItemType == "Music")
            {
                var music = db.Music.FirstOrDefault(m => m.Id == trans.ItemId);
                if (music != null) { music.Status = "available"; db.Music.Update(music); }
            }
            if (trans.ItemType == "Toy")
            {
                var toy = db.Toys.FirstOrDefault(t => t.Id == trans.ItemId);
                if (toy != null) { toy.Status = "available"; db.Toys.Update(toy); }
            }

            db.BorrowTransactions.Update(trans);
            db.SaveChanges();

            return RedirectToAction("ReturnItem");
        }
    }
}
