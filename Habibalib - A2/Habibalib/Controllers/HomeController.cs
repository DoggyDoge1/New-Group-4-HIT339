using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;
using Habibalib.Models;

namespace Habibalib.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext db;

        public HomeController(AppDbContext context)
        {
            db = context;
        }

        public IActionResult Index(string search)
        {
            var books = db.Books.Where(b => b.Status == "available").ToList();
            var music = db.Music.Where(m => m.Status == "available").ToList();
            var toys = db.Toys.Where(t => t.Status == "available").ToList();

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.ToLower();
                books = books.Where(b => b.Name.ToLower().Contains(s)).ToList();
                music = music.Where(m => m.Name.ToLower().Contains(s)).ToList();
                toys = toys.Where(t => t.Name.ToLower().Contains(s)).ToList();
            }

            ViewBag.Books = books;
            ViewBag.Music = music;
            ViewBag.Toys = toys;
            ViewBag.SearchText = search;

            return View();
        }

        public IActionResult Details(string type, int id)
        {
            Item found = null;

            if (type == "Book") found = db.Books.FirstOrDefault(x => x.Id == id);
            if (type == "Music") found = db.Music.FirstOrDefault(x => x.Id == id);
            if (type == "Toy") found = db.Toys.FirstOrDefault(x => x.Id == id);

            if (found == null) return NotFound();

            return View(found);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
