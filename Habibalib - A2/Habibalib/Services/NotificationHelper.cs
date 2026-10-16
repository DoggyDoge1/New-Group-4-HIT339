using System;
using System.Linq;
using Habibalib.Data;
using Habibalib.Models;

namespace Habibalib.Services
{
    // small helper that pretends to send emails/sms. really it just writes a
    // row into the Notifications table and prints a line to the console
    public static class NotificationHelper
    {
        public static void Send(AppDbContext db, int borrowerId, string borrowerName, string channel, string notifyType, string message)
        {
            var note = new Notification
            {
                BorrowerId = borrowerId,
                BorrowerName = borrowerName,
                Channel = channel,
                NotifyType = notifyType,
                Message = message,
                SentDate = DateTime.Now
            };
            db.Notifications.Add(note);
            db.SaveChanges();

            // this is our console "gateway", pretend this line is a real email/sms going out
            Console.WriteLine("[" + channel + " -> " + borrowerName + "] " + message);
        }

        // checks every active loan for due-soon or overdue, and logs a reminder for
        // each one, but only once per loan so people dont get spammed every sweep
        public static void RunDueDateSweep(AppDbContext db)
        {
            var now = DateTime.Now;
            var activeLoans = db.BorrowTransactions.Where(t => t.ReturnDate == null).ToList();

            foreach (var loan in activeLoans)
            {
                if (loan.DueSoonNotifiedAt == null && loan.DueDate >= now && loan.DueDate <= now.AddDays(2))
                {
                    var borrower = db.Borrowers.FirstOrDefault(b => b.Id == loan.BorrowerId);
                    string name = borrower != null ? borrower.Name : "patron";

                    Send(db, loan.BorrowerId, name, "Email", "DueSoon",
                        "Reminder: \"" + loan.ItemName + "\" is due back on " + loan.DueDate.ToShortDateString() + ".");

                    loan.DueSoonNotifiedAt = now;
                    db.SaveChanges();
                }

                if (loan.OverdueNotifiedAt == null && loan.DueDate < now)
                {
                    var borrower = db.Borrowers.FirstOrDefault(b => b.Id == loan.BorrowerId);
                    string name = borrower != null ? borrower.Name : "patron";
                    int lateDays = (now - loan.DueDate).Days;
                    decimal fineSoFar = lateDays * 10;

                    Send(db, loan.BorrowerId, name, "SMS", "Overdue",
                        "\"" + loan.ItemName + "\" is overdue. A fine of $" + fineSoFar + " is accruing, please return it soon.");

                    loan.OverdueNotifiedAt = now;
                    db.SaveChanges();
                }
            }
        }

        // tells whoever is next on the waitlist that their item is free now
        public static void NotifyNextInQueue(AppDbContext db, string itemType, int itemId)
        {
            var next = db.Reservations
                .Where(r => r.ItemType == itemType && r.ItemId == itemId && r.Status == "waiting")
                .OrderBy(r => r.ReservedDate)
                .FirstOrDefault();

            if (next == null)
            {
                return;
            }

            next.Status = "notified";
            next.NotifiedDate = DateTime.Now;
            db.SaveChanges();

            Send(db, next.BorrowerId, next.BorrowerName, "Email", "Reservation",
                "Good news! \"" + next.ItemName + "\" is now available. Please visit the library within 3 days to borrow it.");
        }
    }
}
