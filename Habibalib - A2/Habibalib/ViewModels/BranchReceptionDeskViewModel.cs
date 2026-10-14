using Habibalib.Models;

namespace Habibalib.ViewModels
{
    public class BranchReceptionDeskViewModel
    {
        public Branch ActiveBranch { get; set; }
        public IReadOnlyList<Branch> Branches { get; set; }
        public IReadOnlyList<Book> LocalBooks { get; set; }
        public IReadOnlyList<BranchTransfer> IncomingTransfers { get; set; }
        public IReadOnlyList<BranchTransfer> OutgoingTransfers { get; set; }
    }
}
