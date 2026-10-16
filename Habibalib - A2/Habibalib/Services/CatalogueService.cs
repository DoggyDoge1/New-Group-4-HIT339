using Habibalib.Data;
using Habibalib.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Habibalib.Services;

public class CatalogueService(AppDbContext db)
{
    public List<CatalogueItemDto> GetItems()
    {
        var books = db.Books.AsNoTracking().Include(b => b.CurrentBranch).ToList()
            .Select(b => new CatalogueItemDto(b.Id, "Book", b.Name, b.Description, b.LibraryCode,
                b.Genre, b.Status, b.Author, b.Isbn, b.CurrentBranchId, b.CurrentBranch?.Name, b.IsInTransit));
        var music = db.Music.AsNoTracking().ToList()
            .Select(m => new CatalogueItemDto(m.Id, "Music", m.Name, m.Description, m.LibraryCode,
                m.Genre, m.Status, m.Artist, null, null, null, false));
        var toys = db.Toys.AsNoTracking().ToList()
            .Select(t => new CatalogueItemDto(t.Id, "Toy", t.Name, t.Description, t.LibraryCode,
                t.ToyType, t.Status, null, null, null, null, false));
        return books.Concat(music).Concat(toys).ToList();
    }
}
