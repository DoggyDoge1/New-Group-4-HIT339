using Microsoft.EntityFrameworkCore;
using Habibalib.Models;

namespace Habibalib.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Music> Music { get; set; }
        public DbSet<Toy> Toys { get; set; }
        public DbSet<Borrower> Borrowers { get; set; }
        public DbSet<BorrowTransaction> BorrowTransactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<BranchTransfer> BranchTransfers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Branch>()
                .HasIndex(branch => branch.Code)
                .IsUnique();

            modelBuilder.Entity<Book>()
                .HasOne(book => book.CurrentBranch)
                .WithMany(branch => branch.CurrentBooks)
                .HasForeignKey(book => book.CurrentBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Book>()
                .HasOne(book => book.HomeBranch)
                .WithMany(branch => branch.HomeBooks)
                .HasForeignKey(book => book.HomeBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BranchTransfer>()
                .HasOne(transfer => transfer.Book)
                .WithMany()
                .HasForeignKey(transfer => transfer.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BranchTransfer>()
                .HasOne(transfer => transfer.FromBranch)
                .WithMany()
                .HasForeignKey(transfer => transfer.FromBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BranchTransfer>()
                .HasOne(transfer => transfer.ToBranch)
                .WithMany()
                .HasForeignKey(transfer => transfer.ToBranchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
