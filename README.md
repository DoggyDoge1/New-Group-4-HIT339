# New-Group-4-HIT339

Habibalib library system (ASP.NET Core MVC, EF Core, SQLite), including a public self-service **Kiosk**.

## Requirements

- Git
- .NET SDK 8.0 or newer (8, 9 or 10 all work). Check with `dotnet --list-sdks`.
  Download: https://dotnet.microsoft.com/download

## Clone and run (step by step)

1. **Install Git** from https://git-scm.com/downloads (check with `git --version`).
2. **Install the .NET SDK** from https://dotnet.microsoft.com/download (8.0 or newer). Check with `dotnet --list-sdks`.
3. **Clone the repo.** On https://github.com/DoggyDoge1/New-Group-4-HIT339 click the green **Code** button and copy the HTTPS URL, then open a terminal (PowerShell, Terminal or VS Code) in the folder where you want the project:

   ```powershell
   git clone https://github.com/DoggyDoge1/New-Group-4-HIT339.git
   ```

   If the repo is private, sign in to GitHub when prompted, and ask the owner to add you as a collaborator first.
4. **Open the project folder:**

   ```powershell
   cd "New-Group-4-HIT339\Habibalib - A2\Habibalib"
   ```
5. **Run it** (the first run downloads packages, builds, and creates the database):

   ```powershell
   dotnet run --urls http://localhost:5080
   ```
6. **Open in a browser** once you see "Now listening on: http://localhost:5080":
   - Library site: http://localhost:5080
   - Kiosk: http://localhost:5080/Kiosk (sign in with card `1001`)
7. **Stop the server** with `Ctrl+C` in the terminal.

To get later changes from the team, run `git pull` in the repo folder, then `dotnet run` again.

If the kiosk pages are missing, you cloned a branch that doesn't have them yet. Run `git branch -a` and `git checkout <branch-name>`, or ask the owner to push the kiosk commit.

The SQLite database (`libdata.db`) is created and seeded automatically on first start.

### Which .NET do I need?

Any **.NET SDK 8.0 or newer** (8, 9 or 10) works. The project targets .NET 8 and has `<RollForward>Major</RollForward>` in `Habibalib.csproj`, so it runs on a newer runtime automatically, with nothing extra to install or configure.

Check what you have with `dotnet --list-sdks`. If it prints nothing or the command isn't found, install the SDK from https://dotnet.microsoft.com/download.

### Still get "You must install or update .NET"?

This only happens if your copy of `Habibalib.csproj` lacks the `RollForward` line (for example an older clone). Run `git pull` to update, or use **either** option:

**Option A: run on the newer .NET.** Run both lines in the same terminal, from `Habibalib - A2\Habibalib`:

```powershell
$env:DOTNET_ROLL_FORWARD = "Major"
dotnet run --urls http://localhost:5080
```

macOS/Linux:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --urls http://localhost:5080
```

The setting only lasts for that terminal window.

**Option B: install .NET 8.** Download the **.NET 8 SDK** from https://dotnet.microsoft.com/download/dotnet/8.0. It installs alongside newer versions without conflict.

### Port already in use?

If you see "address already in use" or the app fails to start, another copy is already running on port 5080. Stop it with `Ctrl+C` in its terminal, or run on another port, for example `dotnet run --urls http://localhost:5090` (then browse to port 5090).

### Resetting the data

Stop the server and delete `libdata.db`; it is recreated and reseeded on the next start.

## Staff logins (library site)

| Role | Email | Password |
|---|---|---|
| Receptionist | recept@gmail.com | pwdrecept@999 |
| Admin | admin@gmail.com | pwdadmin@999 |

## Kiosk

A public, self-service screen with no site navigation, header, footer or admin links.

**Demo card:** `1001` (John Smith).

### Patron journey

1. **Welcome screen:** scan your library card, or tap **Enter Card Manually** to open the on-screen numpad.
2. **Dashboard:** shows "Welcome, John!", items **scanned this session**, items **already checked out** (due dates, overdue in red) and outstanding fines.
3. **Check out:** scan a book barcode (`B001`, `B002`, `B003`, `M001`...). The screen flashes green with a beep: "Checked Out! Due back in 14 days". Use **Remove** to undo a mistaken scan.
4. **Return:** switch to the Return tab and scan the item.
5. **End:** tap **Finish & Print Receipt** (shows an on-screen receipt), **Cancel** or **Logout**. If the patron walks away, the kiosk resets to the welcome screen after **60 seconds** of inactivity.

**Call for Staff** and **Help** are in the top bar of every kiosk screen.

### Scanners and touch

- USB/Bluetooth barcode scanners work as a keyboard: the input is focused automatically, and the scanner's Enter key submits.
- Tap targets are 48-96px high. The on-screen numpad is a centred pop-up for patrons without a card (digits only).

### Sessions and privacy

- No cookies. A random session token is held in server memory and passed in the page, so nothing persists in the browser.
- Sessions expire after 60 seconds of inactivity or on Finish/Cancel/Logout. Old links show "Your session timed out".

### Errors

Kiosk errors (server errors and unknown `/Kiosk/...` URLs) show a kiosk-styled error page with a **Back to Start** button and an automatic return after 20 seconds. This is handled only for kiosk pages by `Filters/KioskExceptionFilter.cs` and `Views/Kiosk/KioskError.cshtml`.

### Kiosk files

- `Controllers/KioskController.cs`: logic and models
- `Views/Kiosk/`: `KioskIndex`, `KioskMenu`, `KioskReceipt`, `KioskError`, `_KioskHeader`, `_KioskNumpad`
- `wwwroot/css/kiosk.css`: styling
- `Filters/KioskExceptionFilter.cs`: kiosk error handling

### Notes

- Checked-out items are set to "borrowed", so they disappear from the library home page until returned.
- There is no PIN: the card number alone signs in (demo only).