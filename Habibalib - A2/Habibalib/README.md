# Habibalib - Library Management System

This is my Assignment 3 group project. It is a modified version library system of assignment 2 made in ASP.NET Core MVC with SQLite database.

## What the project can do

- Admin can add, edit and delete books, music and toys.
- Reception can manage borrowers, borrow and return items, and handle late fines.
- Reception can view branch book inventory and request, dispatch and receive inter-branch transfers.
- Managers can view item statistics, borrowing totals and fine totals.
- The public can search the catalogue and view item details without signing in.
- Patrons can use the self-service kiosk to borrow and return items.

## Requirements

- Git
- .NET SDK 8.0 or newer. The app targets .NET 8; the project is configured to roll forward to a newer major runtime.

Check your installed SDKs with `dotnet --list-sdks`. Download the SDK from https://dotnet.microsoft.com/download.

## Clone and run

1. Clone the repository:

   ```powershell
   git clone https://github.com/DoggyDoge1/New-Group-4-HIT339.git
   ```

2. Open the project directory:

   ```powershell
   cd "New-Group-4-HIT339\Habibalib - A2\Habibalib"
   ```

3. Restore packages and run the app:

   ```powershell
   dotnet restore
   dotnet run --urls http://localhost:5080
   ```

4. Open the library site at http://localhost:5080 or the kiosk at http://localhost:5080/Kiosk.
5. Stop the app with `Ctrl+C`.

You can also open `Habibalib.csproj` in Visual Studio 2022 and run the project there. The first start creates and seeds the SQLite database. To get later team changes, run `git pull` from the repository folder and restart the app.

If you see "address already in use", stop the other app instance or use a different port, for example:

```powershell
dotnet run --urls http://localhost:5090
```

The setting below can be used as a workaround with older copies of the project that lack the runtime roll-forward setting. Run both lines in the same PowerShell terminal:

```powershell
$env:DOTNET_ROLL_FORWARD = "Major"
dotnet run --urls http://localhost:5080
```

The environment variable only applies to that terminal window. On macOS/Linux, use `DOTNET_ROLL_FORWARD=Major dotnet run --urls http://localhost:5080`.

## Staff logins

| Role | Email | Password |
|---|---|---|
| Reception | recept@gmail.com | pwdrecept@999 |
| Admin | admin@gmail.com | pwdadmin@999 |
| Manager | manager@gmail.com | pwd@999 |

## Branch transfers

Reception staff can coordinate book transfers between the Central, North and West branches from the **Branch Transfers** link in the library site navigation.

### Select the reception desk's branch

1. Sign in with the Reception account and open **Branch Transfers**.
2. Select the branch this reception desk represents: Central City Library, North Campus Library or West End Community Hub.
3. The selection is remembered for the current browser session. Change it from the dropdown when using another branch desk.

The default physical branch is configured in `appsettings.json`:

```json
"BranchSettings": {
  "LocationBranchId": 1
}
```

Branch IDs are Central `1`, North `2`, and West `3`. The configured branch is used as the fallback for reception sessions and as the kiosk's physical branch. Admin-created books are assigned to this branch as both their current and home branch. Switching the reception desk dropdown does not change the app setting.

### Request, dispatch, and receive a book

1. At the sending branch, find the book in **Local inventory**. Only available books can be transferred.
2. Choose a destination branch, optionally add a note, and click **Request**. The book is marked as in transit and cannot be checked out while the transfer is pending.
3. In **Outgoing transfers**, click **Dispatch** when the book physically leaves. Its transfer status changes from `Requested` to `InTransit`.
4. Switch the desk to the destination branch. The dispatched transfer appears under **Incoming transfers**.
5. Click **Confirm received** when the book arrives. The transfer becomes `Received`, the book's current branch changes to the destination, and the in-transit lock is removed.

The transfer must be dispatched before it can be received. Only the sending branch can dispatch it, and only the destination branch can confirm receipt. A transfer does not change the book's home branch. Music and toys are not currently included in branch transfers.

Invalid transfer actions display a Branch Transfer error page with an explanation and a link back to the reception desk. Unexpected errors are logged and shown as a generic message so technical details are not exposed.

## Kiosk

The kiosk is a public self-service screen without the normal site navigation, header, footer or staff links.

**Demo card:** `1001` (John Smith).

### Patron journey

1. On the welcome screen, scan a library card or choose **Enter Card Manually** to use the on-screen numpad.
2. The dashboard shows the patron's name, items scanned in this session, existing checkouts with due dates, and outstanding fines.
3. To borrow, scan an item barcode such as `B001`, `B002`, `B003`, or `M001`. Use **Remove** to undo a checkout made during the session.
4. To return an item, switch to the Return tab and scan its code.
5. Finish with **Finish & Print Receipt**, **Cancel**, or **Logout**. The kiosk returns to the welcome screen after 60 seconds of inactivity.

**Call for Staff** and **Help** are available in the kiosk top bar.

### Scanners, sessions, and privacy

- USB/Bluetooth barcode scanners work as keyboards; the kiosk focuses the input and the scanner's Enter key submits it.
- The on-screen numpad is available for touch users.
- Kiosk sessions use a random server-side token rather than browser cookies and expire after 60 seconds of inactivity or when ended.
- There is no PIN; the demo card number alone signs in.

Kiosk errors and unknown `/Kiosk/...` URLs display a kiosk-styled error page with a **Back to Start** button and automatic return after 20 seconds.

## Database and project notes

- The SQLite database is `libdata.db`. It is created and seeded automatically on first start; stop the app before deleting the file to reset the data.
- Startup initialization creates/seeds the Central, North and West branches and adds the branch-transfer tables and book-location columns to an existing SQLite database. Existing books without a location are assigned to Central.
- Books in transit are excluded from checkout and public availability until the receiving branch confirms delivery.
- Passwords are stored as plain text for this assignment. A real application should store password hashes.
- `Item` is the base class for `Book`, `Music` and `Toy`.
- Late returns accrue a fine of 10 per day; the loan period is 14 days.
- Admin, Reception and Manager pages require the corresponding staff role. Public catalogue and kiosk pages do not require staff login.
