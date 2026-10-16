# Member 3: implementation and demonstration guide

This branch adds the public read-only API, CSV catalogue importing and manager reports to the team's existing Habibalib application. It targets .NET 10 and is integrated against main commit 0db9100, preserving the kiosk, branch transfers, reservations and notification modules. The revised patch supersedes the original member3-changes.patch, whose navigation hunk conflicted with newer team links.

## 1. Prepare your computer

Install the .NET 10 SDK, Git and an editor such as Visual Studio Code. Run `dotnet --version` and check that it starts with `10.`. An older Visual Studio installation may not support this target; the SDK commands below work independently of the editor.

If you downloaded the completed source ZIP, extract it and open a terminal in `Habibalib - A2/Habibalib`. If working in the team's Git clone, first review/apply the supplied patch on a new branch, as described in the delivery notes. The branch is local until you push it; it is not already on GitHub.

## 2. Build and start the application

In PowerShell, from the folder containing `Habibalib.csproj`:

```powershell
dotnet restore
dotnet build
# Create a local demonstration key. Set the same value in any terminal used for API requests.
$env:PublicApi__ApiKey = [guid]::NewGuid().ToString('N')
dotnet run --no-launch-profile --urls http://localhost:5080
```

For Bash:

```bash
export PublicApi__ApiKey="$(openssl rand -hex 32)"
dotnet restore
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5080
```

Keep the terminal open. Visit `http://localhost:5080`. Stop with Ctrl+C. The SQLite database is created and seeded on first start. An unset API key leaves API requests unauthorised; there is no default key committed to the repository. Use HTTPS when deploying outside localhost.

## 3. Demonstrate the public API

1. Open `http://localhost:5080/swagger`.
2. Select **Authorize**, enter the local API key and confirm.
3. Try `GET /api/v1/items`. This returns only available items and excludes books in transit.
4. Try filters such as `type=Book`, `branchId=2`, `search=handbook`, `category=Computing`, `page=1`, and `pageSize=20`.
5. Try `GET /api/v1/categories` and `GET /api/v1/status`.
6. Remove the key and repeat a request: expect HTTP 401, not a staff-login HTML page.
7. Invalid paging returns HTTP 400. There are no POST, PUT or DELETE API actions. Requests are limited to 120 per minute per remote IP.

The categories are existing book/music genres and toy types; there is no separate category table. Status reports the configured service state and branch directory, not live physical opening hours. Set `PublicApi:OperatingStatus` in configuration if required. Branch filtering excludes music/toys because those types have no branch assignment in the baseline.

PowerShell example (in a terminal with the same key):

```powershell
Invoke-RestMethod 'http://localhost:5080/api/v1/items' -Headers @{'X-Api-Key'=$env:PublicApi__ApiKey}
```

## 4. Demonstrate importing

1. Sign in as the seeded Admin listed in the main README.
2. Open **Import catalogue** or `/Import`.
3. Download the sample CSV and upload it unchanged: expect three imported items.
4. Check **Manage Items**. The sample book is assigned to NORTH as both its current and home branch. All imported items start available.
5. Upload the sample again: expect duplicate errors and no further additions.
6. Change one book's BranchCode to `MISSING`, give the rows new codes and retry: the whole file must be rejected.

The header order is fixed. Quoted fields, commas, Unicode and quoted newlines are supported. The limit is 1 MB and 1,000 data rows. Books need an author and existing branch code; music needs artist and valid year; toys need toy type and an age from 0 to 120. Codes must be unique across all types and within the upload, ignoring case. The upload uses an anti-forgery token and a database transaction. No external book service is required: CSV is the selected alternative allowed by the brief.

## 5. Demonstrate reports and exports

1. Sign out and sign in as Manager.
2. Open **Reports & exports** or `/Manager/Reports`.
3. Select **borrowing** and optionally set an inclusive borrowing-date range.
4. Select **fines** to see charged, paid and outstanding values in AUD.
5. Clear the dates and select **inventory** to see a current inventory snapshot, including book branches and in-transit flags.
6. Download CSV and PDF for each report. The report scope and summary are included in both exports.

Fine reports select loans by borrowing date and show their current payment state. The baseline stores no payment timestamp, so the report cannot establish revenue received during a payment-date interval. It uses recorded fines; it does not create new fines for active overdue loans. Historical loan records do not store the branch at borrowing time, so borrowing/fine reports do not offer a misleading historical branch filter.

The existing manager summary page remains available at `/Manager`. CSV output preserves UTF-8, escapes quotes/commas and prefixes formula-like values so spreadsheet applications treat them as text. PDF output uses QuestPDF under its Community licence for this educational project.

## 6. Run the integration checks

Install Python 3, then run from this project folder:

```powershell
python scripts/verify_member3.py
```

The script builds the application, starts it on a free local port and creates a fresh database in a temporary directory. It never uses or changes your working `libdata.db`. It checks API access, read-only methods, Swagger, role restrictions, anti-forgery validation, successful/failed imports, filters, fine totals and all report exports. It also checks borrowing/returning with a two-person reservation queue, notifications and API availability. Test fixtures are synthetic and exist only in that temporary database.

## 7. Understand where the code lives

| File or folder | Purpose |
|---|---|
| `Security/ApiKeyAuthenticationHandler.cs` | Separate API-key authentication returning HTTP 401 |
| `Controllers/CatalogueApiController.cs` | The three read-only API routes |
| `Services/CatalogueService.cs` | Catalogue projection shared by API and inventory reports |
| `Controllers/ImportController.cs` | Admin-only upload and sample download |
| `Services/CsvImportService.cs` | Parsing, validation, duplicate checks and transaction |
| `Controllers/ManagerController.cs` | Existing summary plus report and export actions |
| `Services/ReportService.cs` | Report filtering and calculations |
| `Services/ReportExportService.cs` | CSV escaping and PDF layout |
| `Models/Integrations` | API response, import result and report models |
| `Views/Import` and `Views/Manager/Reports.cshtml` | Staff screens |
| `Samples/catalogue-import.csv` | Demonstration input |
| `Program.cs` | Registers the new services, authentication, Swagger and rate limiter |

## 8. Collect assessment evidence

Capture the successful build, Swagger success and 401 responses, valid/invalid import results, imported catalogue items, each report and its downloaded exports. Record your actual work hours and Git contributions. Test the installation instructions on the assessment VM with your group. Screenshots and a local build alone do not establish that the VM instructions work.
