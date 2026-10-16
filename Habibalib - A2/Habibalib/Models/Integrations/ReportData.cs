using System.ComponentModel.DataAnnotations;

namespace Habibalib.Models.Integrations;

public class ReportFilter : IValidatableObject
{
    [Required]
    [RegularExpression("^(borrowing|fines|inventory)$")]
    public string Kind { get; set; } = "borrowing";
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From.Value.Date > To.Value.Date)
            yield return new ValidationResult("From date must be on or before To date.", new[] { nameof(From), nameof(To) });
        if (Kind == "inventory" && (From.HasValue || To.HasValue))
            yield return new ValidationResult("Inventory is a current snapshot. Clear the dates to view it.");
    }
}

public class ReportData
{
    public ReportFilter Filter { get; set; } = new();
    public string Title { get; set; } = "Manager reports";
    public string Scope { get; set; } = "";
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Summary { get; } = new();
    public string[] Columns { get; set; } = Array.Empty<string>();
    public List<string[]> Rows { get; set; } = new();
}
