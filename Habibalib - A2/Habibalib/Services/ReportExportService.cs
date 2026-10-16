using System.Text;
using Habibalib.Models.Integrations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Habibalib.Services;

public class ReportExportService
{
    private static string CsvCell(string value)
    {
        value ??= "";
        // Quoting alone does not stop spreadsheet formula execution.
        var first = value.TrimStart();
        if (first.Length > 0 && "=+-@".Contains(first[0]) || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public byte[] Csv(ReportData report)
    {
        var text = new StringBuilder();
        void Row(IEnumerable<string> cells) => text.AppendLine(string.Join(",", cells.Select(CsvCell)));
        Row(new[] { report.Title });
        Row(new[] { "Generated UTC", report.GeneratedUtc.ToString("O") });
        Row(new[] { "Scope", report.Scope });
        foreach (var item in report.Summary) Row(new[] { item.Key, item.Value });
        text.AppendLine();
        Row(report.Columns);
        foreach (var row in report.Rows) Row(row);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }

    public byte[] Pdf(ReportData report) => Document.Create(document =>
    {
        document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(style => style.FontSize(9));
            page.Header().PaddingBottom(12).Column(column =>
            {
                column.Item().Text("HABIBALIB | " + report.Title).FontSize(19).SemiBold().FontColor(Colors.Blue.Darken3);
                column.Item().Text("Generated UTC: " + report.GeneratedUtc.ToString("yyyy-MM-dd HH:mm"));
                column.Item().Text(report.Scope).FontSize(8);
            });
            page.Content().Column(column =>
            {
                foreach (var summary in report.Summary)
                    column.Item().Text($"{summary.Key}: {summary.Value}");
                column.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in report.Columns) columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        foreach (var title in report.Columns)
                            header.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(title).SemiBold();
                    });
                    foreach (var row in report.Rows)
                        foreach (var cell in row)
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cell ?? "");
                });
                if (report.Rows.Count == 0) column.Item().PaddingTop(10).Text("No records match this report.");
            });
            page.Footer().AlignRight().Text(text =>
            {
                text.Span("Page "); text.CurrentPageNumber(); text.Span(" of "); text.TotalPages();
            });
        });
    }).GeneratePdf();
}
