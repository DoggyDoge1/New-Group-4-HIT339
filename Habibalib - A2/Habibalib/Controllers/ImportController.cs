using Habibalib.Models.Integrations;
using Habibalib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Habibalib.Controllers;

[Authorize(Roles = "Admin")]
public class ImportController(CsvImportService importer, IWebHostEnvironment environment,
    ILogger<ImportController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new ImportResult());

    [HttpGet]
    public IActionResult Sample() => PhysicalFile(Path.Combine(environment.ContentRootPath, "Samples", "catalogue-import.csv"),
        "text/csv; charset=utf-8", "catalogue-import.csv");

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(CsvImportService.MaximumBytes + 65536)]
    public IActionResult Index(IFormFile file)
    {
        if (file == null || file.Length == 0 || file.Length > CsvImportService.MaximumBytes ||
            !string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            var invalid = new ImportResult();
            invalid.Errors.Add("Choose a non-empty .csv file no larger than 1 MB.");
            return View(invalid);
        }
        try
        {
            using var stream = file.OpenReadStream();
            return View(importer.Import(stream));
        }
        catch (Exception exception) when (exception is Microsoft.EntityFrameworkCore.DbUpdateException
            or Microsoft.Data.Sqlite.SqliteException or IOException)
        {
            logger.LogError(exception, "Catalogue import failed; transaction rolled back.");
            var failed = new ImportResult();
            failed.Errors.Add("The import could not be saved. No items were imported. Please try again or contact the administrator.");
            return View(failed);
        }
    }
}
