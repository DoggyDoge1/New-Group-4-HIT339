using System.ComponentModel.DataAnnotations;

namespace Habibalib.Models
{
    public enum TransferStatus
    {
        Requested = 0,
        InTransit = 1,
        Received = 2,
        Cancelled = 3
    }

    public class BranchTransfer
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        public int FromBranchId { get; set; }
        public Branch FromBranch { get; set; }

        [Required]
        public int ToBranchId { get; set; }
        public Branch ToBranch { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DispatchedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.Requested;

        [StringLength(250)]
        public string Notes { get; set; }

        public string RequestedByStaffId { get; set; }
    }
}
