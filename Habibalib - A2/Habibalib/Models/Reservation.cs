using System;

namespace Habibalib.Models
{
    // one row per patron waiting for a borrowed or damaged item
    public class Reservation
    {
        public int Id { get; set; }
        public string ItemType { get; set; } // Book, Music or Toy
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int BorrowerId { get; set; }
        public string BorrowerName { get; set; }
        public DateTime ReservedDate { get; set; }
        public string Status { get; set; } = "waiting"; // waiting, notified, fulfilled, cancelled
        public DateTime? NotifiedDate { get; set; }
    }
}
