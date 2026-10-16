using System.ComponentModel.DataAnnotations;
using Habibalib.Data;
using Habibalib.Models.Integrations;
using Habibalib.Security;
using Habibalib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Habibalib.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[EnableRateLimiting("catalogue")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public class CatalogueApiController(CatalogueService catalogue, AppDbContext db, IConfiguration configuration) : ControllerBase
{
    /// <summary>List available catalogue items. Books in transit are excluded.</summary>
    [HttpGet("items")]
    [ProducesResponseType(typeof(CataloguePage), StatusCodes.Status200OK)]
    public ActionResult<CataloguePage> Items([StringLength(200)] string search = null,
        [RegularExpression("^(Book|Music|Toy)$")] string type = null,
        [StringLength(200)] string category = null, [Range(1, int.MaxValue)] int? branchId = null,
        [Range(1, int.MaxValue)] int page = 1, [Range(1, 100)] int pageSize = 20)
    {
        var items = catalogue.GetItems().Where(i => i.Status == "available" && !i.IsInTransit);
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(i => i.Name?.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) == true);
        if (type != null) items = items.Where(i => i.Type == type);
        if (category != null) items = items.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));
        // Music/toys have no branch assignment in the existing application.
        if (branchId.HasValue) items = items.Where(i => i.BranchId == branchId);
        var ordered = items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Type).ThenBy(i => i.Id).ToList();
        var offset = ((long)page - 1) * pageSize;
        var results = offset >= ordered.Count ? new List<CatalogueItemDto>() : ordered.Skip((int)offset).Take(pageSize).ToList();
        return Ok(new CataloguePage(page, pageSize, ordered.Count, results));
    }

    /// <summary>List categories derived from book/music genres and toy types.</summary>
    [HttpGet("categories")]
    public IActionResult Categories() => Ok(catalogue.GetItems()
        .Where(i => !string.IsNullOrWhiteSpace(i.Category))
        .GroupBy(i => new { i.Type, i.Category })
        .Select(g => new { g.Key.Type, g.Key.Category, TotalItems = g.Count(),
            AvailableItems = g.Count(i => i.Status == "available" && !i.IsInTransit) })
        .OrderBy(c => c.Type).ThenBy(c => c.Category));

    /// <summary>Read configured service status and branch directory (not opening hours).</summary>
    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        Service = "Habibalib catalogue", ApiVersion = "v1", ReadOnly = true,
        OperatingStatus = configuration["PublicApi:OperatingStatus"] ?? "Operational",
        TimestampUtc = DateTime.UtcNow,
        Branches = db.Branches.OrderBy(b => b.Id).Select(b => new { b.Id, b.Name, b.Code, b.Address, b.PhoneNumber }).ToList()
    });
}
