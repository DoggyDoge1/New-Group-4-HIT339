# Habibalib - Library Management System

This is my Assignment 2 project. It is a simple library system made in ASP.NET Core MVC with SQLite database.

## What this project can do

- Admin can add, edit, delete books, music and toys
- Reception can add borrowers and can borrow/return items, late items get fine
- Manager can see a stats page (available, borrowed, damaged, destroyed counts and fine totals)
- Public people (no login needed) can search items on home page and see item details on a separate page

## Login accounts (already inside database, no need to register)

Admin
email: admin@gmail.com
password: pwdadmin@999

Reception
email: recept@gmail.com
password: pwdrecept@999

Manager
email: manager@gmail.com
password: pwd@999

## How to run

1. Open the Habibalib folder in Visual Studio 2022 (open the .csproj file)
2. Let it restore the nuget packages (needs internet one time for this)
3. Press the green run button (or press F5)
4. Browser opens on home page, that is the public search page
5. Click "Staff Login" on top to login as admin, reception or manager

Or run from terminal:
```
cd Habibalib
dotnet restore
dotnet run
```

## Notes

- Database is SQLite, file called libdata.db, it gets created automatically the first time you run the project. No need to make migrations by hand, code uses EnsureCreated.
- Seed data (some starting books, music, toys and the 3 login accounts) get added automatically on first run.
- Passwords are stored as plain text in this project because it is just for assignment purpose. In a real project passwords should be hashed.
- Item, Book, Music, Toy classes use inheritance like the assignment asked. Book/Music/Toy each get their own table in database.
- Fine for late return is 10 per day late. Due date is 14 days after the borrow date.
- Admin, Reception, Manager pages are protected with role based login, Public search page does not need login.

Built by Habiba
