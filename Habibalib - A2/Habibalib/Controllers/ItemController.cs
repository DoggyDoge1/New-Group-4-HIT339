using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;
using Habibalib.Models;

namespace Habibalib.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ItemController : Controller
    {
        private readonly AppDbContext db;

        public ItemController(AppDbContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            ViewBag.Books = db.Books.ToList();
            ViewBag.Music = db.Music.ToList();
            ViewBag.Toys = db.Toys.ToList();
            return View();
        }

        // ---------- BOOK ----------
        public IActionResult CreateBook()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateBook(Book book)
        {
            db.Books.Add(book);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult EditBook(int id)
        {
            var book = db.Books.FirstOrDefault(b => b.Id == id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        public IActionResult EditBook(Book book)
        {
            db.Books.Update(book);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteBook(int id)
        {
            var book = db.Books.FirstOrDefault(b => b.Id == id);
            if (book != null)
            {
                db.Books.Remove(book);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ---------- MUSIC ----------
        public IActionResult CreateMusic()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateMusic(Music music)
        {
            db.Music.Add(music);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult EditMusic(int id)
        {
            var music = db.Music.FirstOrDefault(m => m.Id == id);
            if (music == null) return NotFound();
            return View(music);
        }

        [HttpPost]
        public IActionResult EditMusic(Music music)
        {
            db.Music.Update(music);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteMusic(int id)
        {
            var music = db.Music.FirstOrDefault(m => m.Id == id);
            if (music != null)
            {
                db.Music.Remove(music);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ---------- TOY ----------
        public IActionResult CreateToy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateToy(Toy toy)
        {
            db.Toys.Add(toy);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult EditToy(int id)
        {
            var toy = db.Toys.FirstOrDefault(t => t.Id == id);
            if (toy == null) return NotFound();
            return View(toy);
        }

        [HttpPost]
        public IActionResult EditToy(Toy toy)
        {
            db.Toys.Update(toy);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteToy(int id)
        {
            var toy = db.Toys.FirstOrDefault(t => t.Id == id);
            if (toy != null)
            {
                db.Toys.Remove(toy);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
