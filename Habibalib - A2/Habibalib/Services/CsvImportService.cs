using System.Globalization;
using System.Text;
using Habibalib.Data;
using Habibalib.Models;
using Habibalib.Models.Integrations;
using Microsoft.VisualBasic.FileIO;

namespace Habibalib.Services;

public class CsvImportService(AppDbContext db)
{
    public const int MaximumRows = 1000;
    public const long MaximumBytes = 1024 * 1024;
    public static readonly string[] Headers = { "Type", "LibraryCode", "Name", "Description", "Author", "Genre", "Isbn", "Artist", "Year", "ToyType", "Age", "BranchCode" };

    public ImportResult Import(Stream stream)
    {
        var result = new ImportResult();
        // Validate the whole file before writing: an invalid file imports nothing.
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true, 4096, true);
        using var parser = new TextFieldParser(reader) { TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(",");
        var pending = new List<Item>();
        try
        {
            if (parser.EndOfData || !(parser.ReadFields() ?? Array.Empty<string>()).SequenceEqual(Headers, StringComparer.OrdinalIgnoreCase))
            {
                result.Errors.Add("The header must match the sample CSV, with all columns in the same order.");
                return result;
            }

            using var transaction = db.Database.BeginTransaction();
            var codes = db.Books.Select(b => b.LibraryCode).ToList()
                .Concat(db.Music.Select(m => m.LibraryCode).ToList())
                .Concat(db.Toys.Select(t => t.LibraryCode).ToList())
                .Select(c => c?.Trim() ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);
            var branches = db.Branches.ToList();
            var row = 1;
            while (!parser.EndOfData)
            {
                row++;
                if (row - 1 > MaximumRows)
                {
                    result.Errors.Add($"A file may contain at most {MaximumRows} items.");
                    break;
                }
                var fields = parser.ReadFields();
                if (fields == null || fields.Length != Headers.Length)
                {
                    result.Errors.Add($"Row {row}: expected {Headers.Length} columns.");
                    continue;
                }
                if (fields.Any(f => f.Length > 2000) || fields[1].Length > 100 || fields[2].Length > 200)
                {
                    result.Errors.Add($"Row {row}: code must be at most 100 characters, name 200, and other fields 2000.");
                    continue;
                }
                var type = fields[0].ToLowerInvariant();
                var code = fields[1];
                var name = fields[2];
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add($"Row {row}: LibraryCode and Name are required.");
                    continue;
                }
                if (!codes.Add(code))
                {
                    result.Errors.Add($"Row {row}: library code '{code}' already exists or is repeated in this file.");
                    continue;
                }

                Item item;
                switch (type)
                {
                    case "book":
                        var branch = branches.FirstOrDefault(b => string.Equals(b.Code, fields[11], StringComparison.OrdinalIgnoreCase));
                        if (branch == null || string.IsNullOrWhiteSpace(fields[4]))
                        {
                            result.Errors.Add($"Row {row}: a book needs an Author and a valid BranchCode.");
                            continue;
                        }
                        item = new Book { Author = fields[4], Genre = fields[5], Isbn = fields[6],
                            CurrentBranchId = branch.Id, HomeBranchId = branch.Id };
                        break;
                    case "music":
                        if (string.IsNullOrWhiteSpace(fields[7]) || !int.TryParse(fields[8], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
                            || year < 1 || year > DateTime.UtcNow.Year + 1 || !string.IsNullOrEmpty(fields[11]))
                        {
                            result.Errors.Add($"Row {row}: music needs an Artist, valid Year, and blank BranchCode (music has no branch field).");
                            continue;
                        }
                        item = new Music { Artist = fields[7], Year = year, Genre = fields[5] };
                        break;
                    case "toy":
                        if (string.IsNullOrWhiteSpace(fields[9]) || !int.TryParse(fields[10], NumberStyles.None, CultureInfo.InvariantCulture, out var age)
                            || age < 0 || age > 120 || !string.IsNullOrEmpty(fields[11]))
                        {
                            result.Errors.Add($"Row {row}: a toy needs a ToyType, Age from 0 to 120, and blank BranchCode.");
                            continue;
                        }
                        item = new Toy { ToyType = fields[9], Age = age };
                        break;
                    default:
                        result.Errors.Add($"Row {row}: Type must be Book, Music, or Toy.");
                        continue;
                }
                item.LibraryCode = code;
                item.Name = name;
                item.Description = fields[3];
                item.Status = "available";
                pending.Add(item);
            }
            if (row == 1) result.Errors.Add("The CSV contains a header but no items.");
            if (result.Errors.Count > 0) return result;

            foreach (var item in pending)
            {
                if (item is Book book) db.Books.Add(book);
                else if (item is Music music) db.Music.Add(music);
                else if (item is Toy toy) db.Toys.Add(toy);
            }
            db.SaveChanges();
            transaction.Commit();
            result.Imported = pending.Count;
            return result;
        }
        catch (MalformedLineException)
        {
            result.Errors.Add($"Invalid CSV quoting near line {parser.ErrorLineNumber}. No items were imported.");
            return result;
        }
        catch (DecoderFallbackException)
        {
            result.Errors.Add("Save the file as UTF-8 CSV and upload it again. No items were imported.");
            return result;
        }
    }
}
