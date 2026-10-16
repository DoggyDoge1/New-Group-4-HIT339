namespace Habibalib.Models.Integrations;

// Deliberately contains catalogue information only, never patrons or staff records.
public record CatalogueItemDto(int Id, string Type, string Name, string Description,
    string LibraryCode, string Category, string Status, string Creator, string Isbn,
    int? BranchId, string BranchName, bool IsInTransit);

public record CataloguePage(int Page, int PageSize, int Total, IReadOnlyList<CatalogueItemDto> Items);
