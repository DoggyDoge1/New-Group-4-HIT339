namespace Habibalib.Models
{
    public class Book : Item
    {
        public string Author { get; set; }
        public string Genre { get; set; }
        public string Isbn { get; set; }

        public int? CurrentBranchId { get; set; }
        public Branch CurrentBranch { get; set; }

        public int? HomeBranchId { get; set; }
        public Branch HomeBranch { get; set; }

        public bool IsInTransit { get; set; }
    }
}
