using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habibalib.Data;
using Habibalib.Services;

namespace Habibalib.Controllers
{
    // dashboard log of every fake email/sms we "sent"
    [Authorize(Roles = "Reception,Manager")]
    public class NotificationController : Controller
    {
        private readonly AppDbContext db;

        public NotificationController(AppDbContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            NotificationHelper.RunDueDateSweep(db);

            var list = db.Notifications.OrderByDescending(n => n.SentDate).Take(100).ToList();
            return View(list);
        }
    }
}
