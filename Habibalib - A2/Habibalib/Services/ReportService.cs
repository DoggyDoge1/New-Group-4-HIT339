using System.Globalization;
using Habibalib.Data;
using Habibalib.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Habibalib.Services;

public class ReportService(AppDbContext db, CatalogueService catalogue)
{
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Date(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    public ReportData Build(ReportFilter filter)
    {
        var report = new ReportData { Filter = filter };
        if (filter.Kind == "inventory")
        {
            var items = catalogue.GetItems().OrderBy(i => i.Type).ThenBy(i => i.Name).ToList();
            report.Title = "Inventory health";
            report.Scope = "Current snapshot. Branch tracking applies to books; music and toys are unassigned.";
            report.Columns = new[] { "Type", "Code", "Item", "Status", "Branch", "In transit" };
            report.Rows = items.Select(i => new[] { i.Type, i.LibraryCode, i.Name, i.Status,
                i.BranchName ?? "Unassigned", i.IsInTransit ? "Yes" : "No" }).ToList();
            report.Summary["Total items"] = items.Count.ToString();
            report.Summary["Available to borrow"] = items.Count(i => i.Status == "available" && !i.IsInTransit).ToString();
            report.Summary["Borrowed"] = items.Count(i => i.Status == "borrowed").ToString();
            report.Summary["Damaged"] = items.Count(i => i.Status == "damaged").ToString();
            report.Summary["Destroyed"] = items.Count(i => i.Status == "destroy").ToString();
            report.Summary["In transit"] = items.Count(i => i.IsInTransit).ToString();
            return report;
        }

        var query = db.BorrowTransactions.AsNoTracking().AsQueryable();
        if (filter.From.HasValue) query = query.Where(t => t.BorrowDate >= filter.From.Value.Date);
        // Comparing date components keeps the inclusive end date safe even at DateTime.MaxValue.
        if (filter.To.HasValue) query = query.Where(t => t.BorrowDate.Date <= filter.To.Value.Date);
        var loans = query.OrderByDescending(t => t.BorrowDate).ThenBy(t => t.Id).ToList();
        report.Scope = $"Loans borrowed from {Date(filter.From) switch { "" => "the beginning", var date => date }} to {Date(filter.To) switch { "" => "the latest record", var date => date }} (inclusive).";
        if (filter.Kind == "fines")
        {
            var fines = loans.Where(t => t.Fine > 0).ToList();
            report.Title = "Fine revenue audit";
            report.Scope += " Payment state is current; payment dates are not recorded, so this is not revenue by payment date.";
            report.Columns = new[] { "Loan", "Item", "Borrowed", "Returned", "Charged", "Paid", "Outstanding" };
            report.Rows = fines.Select(t => new[] { t.Id.ToString(), t.ItemName, Date(t.BorrowDate), Date(t.ReturnDate),
                Money(t.Fine), Money(t.Paid ? t.Fine : 0), Money(t.Paid ? 0 : t.Fine) }).ToList();
            report.Summary["Fines charged (AUD)"] = Money(fines.Sum(t => t.Fine));
            report.Summary["Fines paid (AUD)"] = Money(fines.Where(t => t.Paid).Sum(t => t.Fine));
            report.Summary["Fines outstanding (AUD)"] = Money(fines.Where(t => !t.Paid).Sum(t => t.Fine));
            report.Summary["Loans with fines"] = fines.Count.ToString();
        }
        else
        {
            report.Title = "Borrowing statistics";
            report.Columns = new[] { "Loan", "Type", "Item", "Borrowed", "Due", "Returned", "Loan status" };
            report.Rows = loans.Select(t => new[] { t.Id.ToString(), t.ItemType, t.ItemName,
                Date(t.BorrowDate), Date(t.DueDate), Date(t.ReturnDate),
                t.ReturnDate.HasValue ? "Returned" : t.DueDate < DateTime.Now ? "Overdue" : "Active" }).ToList();
            report.Summary["Total loans"] = loans.Count.ToString();
            report.Summary["Active loans"] = loans.Count(t => !t.ReturnDate.HasValue).ToString();
            report.Summary["Returned loans"] = loans.Count(t => t.ReturnDate.HasValue).ToString();
            report.Summary["Overdue loans"] = loans.Count(t => !t.ReturnDate.HasValue && t.DueDate < DateTime.Now).ToString();
            foreach (var type in new[] { "Book", "Music", "Toy" })
                report.Summary[$"{type} loans"] = loans.Count(t => t.ItemType == type).ToString();
        }
        return report;
    }
}
