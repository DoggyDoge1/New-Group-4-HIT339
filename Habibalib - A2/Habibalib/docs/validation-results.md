# Member 3 validation results

Verified against baseline commit `c66a203e8a624b38c33b3494d588a7baada9d830` with the Member 3 changes on local branch `member3-integrations-analytics`.

## Build and integration checks

- Environment: Linux x64 with .NET SDK 10.0.402; target framework net10.0.
- Build: succeeded with zero warnings and zero errors.
- Command: `python scripts/verify_member3.py` (DOTNET_EXE pointed to the installed SDK).
- Result: 15 test groups passed.
- Data isolation: all automated fixtures were synthetic and stored in a disposable SQLite database. The working database was not used.

| Test group | Result |
|---|---|
| API authentication, including rejecting staff cookies as API credentials | Pass |
| Available items, excluding in-transit books and personal data | Pass |
| API filtering, pagination, invalid values and overflow boundary | Pass |
| Categories, status and rejecting POST/PUT/DELETE/PATCH | Pass |
| Swagger schema, routes and API-key declaration | Pass |
| Staff roles and import anti-forgery enforcement | Pass |
| Successful import, branch assignment and existing-code rejection | Pass |
| Atomic rejection for invalid branches/years/headers | Pass |
| Quoted commas/newlines, Unicode and within-file duplicate detection | Pass |
| File extension, empty input, size, malformed quoting and 1000-row limit | Pass |
| Report date validation, inclusive end date and independent fine totals | Pass |
| All six CSV/PDF exports and CSV formula protection | Pass |
| PDF generation for a longer inventory report | Pass |
| Existing catalogue, kiosk, admin, manager and transfer pages render | Pass |
| Configured API rate limit returns HTTP 429 | Pass |

## PDF visual checks

The three report types were opened through rendered page images. The longer inventory fixture produced six pages; a continuation page was inspected for repeated column headers and page numbering. The checked pages had readable text and no overlapping or clipped cells.

## UI verification limit

Browser automation could not run because the Chromium download failed in this workspace. HTTP tests verified page rendering and endpoints; a manual browser walkthrough remains necessary.

## Remaining team checks

- Execute the installation instructions on the assessment virtual machine.
- Test reservation and notification integration after Member 2 merges their work.
- Review the .NET 10 change with all members before merging.
- Confirm the live project-board link and marker read access.
- Complete real team details, hours, strengths/weaknesses and the final portfolio PDF.

These results verify the stated scenarios. They do not claim VM testing, a completed group portfolio or GitHub publication.

## Integration follow-up against newer team main

The updated Member 3 patch was applied to team main commit `0db9100` (which includes Member 2 reservations and notifications). The shared navigation now preserves Holds Queue and Notifications while adding Developer API, Import catalogue and Reports & exports.

The updated code built with .NET SDK 10.0.402 with zero warnings/errors. All 16 test groups passed, including a new integration scenario: reception borrows Matilda, API excludes it, two patrons reserve it, reception returns it, the first patron becomes notified while the second remains waiting, the notification is logged, the API lists the available item again, and the reservation/notification staff pages render. Fixtures and the database are disposable.

The patch was also checked for clean application to the above team main commit. This does not claim publication to GitHub or assessment VM testing.
