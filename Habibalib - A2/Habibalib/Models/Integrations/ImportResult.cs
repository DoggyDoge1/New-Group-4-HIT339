namespace Habibalib.Models.Integrations;

public class ImportResult
{
    public int Imported { get; set; }
    public List<string> Errors { get; } = new();
}
