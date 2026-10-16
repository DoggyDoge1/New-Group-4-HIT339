using System;

namespace Habibalib.Models
{
    // this is a fake email/sms log, we dont really send anything,
    // we just write a row here so we can show it on a dashboard
    public class Notification
    {
        public int Id { get; set; }
        public int BorrowerId { get; set; }
        public string BorrowerName { get; set; }
        public string Channel { get; set; } // Email or SMS
        public string NotifyType { get; set; } // Borrowed, DueSoon, Overdue, Reservation
        public string Message { get; set; }
        public DateTime SentDate { get; set; }
    }
}
