using System;

namespace Habibalib.Models
{
    public class BorrowTransaction
    {
        public int Id { get; set; }
        public string ItemType { get; set; } // Book, Music or Toy
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int BorrowerId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal Fine { get; set; }
        public bool Paid { get; set; }
    }
}
