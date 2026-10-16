using System.Linq;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Habibalib.Models;

namespace Habibalib.Data
{
    public static class SeedData
    {
        public static void Run(AppDbContext db)
        {
            db.Database.EnsureCreated();
            EnsureMultiBranchSchema(db);
            EnsureNotificationSchema(db);

            var branches = new[]
            {
                new Branch { Id = 1, Name = "Central City Library", Code = "CENTRAL", Address = "100 Main St, City Centre", PhoneNumber = "555-0101" },
                new Branch { Id = 2, Name = "North Campus Library", Code = "NORTH", Address = "45 University Ave, Northside", PhoneNumber = "555-0102" },
                new Branch { Id = 3, Name = "West End Community Hub", Code = "WEST", Address = "88 Sunset Blvd, Westend", PhoneNumber = "555-0103" }
            };

            foreach (var branch in branches)
            {
                if (!db.Branches.Any(existing => existing.Id == branch.Id || existing.Code == branch.Code))
                {
                    db.Branches.Add(branch);
                }
            }
            db.SaveChanges();

            if (!db.Users.Any())
            {
                db.Users.Add(new User { Email = "admin@gmail.com", Password = "pwdadmin@999", Role = "Admin" });
                db.Users.Add(new User { Email = "recept@gmail.com", Password = "pwdrecept@999", Role = "Reception" });
                db.Users.Add(new User { Email = "manager@gmail.com", Password = "pwd@999", Role = "Manager" });
                db.SaveChanges();
            }

            // fixed demo patron so the kiosk can be used without a physical card scanner
            if (!db.Borrowers.Any(b => b.Id == 1001))
            {
                db.Borrowers.Add(new Borrower { Id = 1001, Name = "John Smith", Email = "john@example.com", Phone = "0400 000 000", Address = "1 Library Street" });
                db.SaveChanges();
            }

            if (!db.Books.Any())
            {
                db.Books.Add(new Book { Name = "Anne of Green Gables", Description = "story of a red hair girl called anne", LibraryCode = "B001", Status = "available", Author = "L.M. Montgomery", Genre = "Fiction", Isbn = "111-111", CurrentBranchId = 1, HomeBranchId = 1 });
                db.Books.Add(new Book { Name = "Matilda", Description = "a girl with magic powers", LibraryCode = "B002", Status = "available", Author = "Roald Dahl", Genre = "Fantasy", Isbn = "222-222", CurrentBranchId = 1, HomeBranchId = 1 });
                db.Books.Add(new Book { Name = "Little Women", Description = "story about 4 sisters growing up", LibraryCode = "B003", Status = "available", Author = "Louisa May Alcott", Genre = "Fiction", Isbn = "333-333", CurrentBranchId = 1, HomeBranchId = 1 });
                db.Books.Add(new Book { Name = "Pippi Longstocking", Description = "strong and brave girl story", LibraryCode = "B004", Status = "damaged", Author = "Astrid Lindgren", Genre = "Adventure", Isbn = "444-444", CurrentBranchId = 1, HomeBranchId = 1 });
                db.SaveChanges();
            }

            foreach (var book in db.Books.Where(book => book.CurrentBranchId == null || book.HomeBranchId == null))
            {
                book.CurrentBranchId ??= 1;
                book.HomeBranchId ??= book.CurrentBranchId;
            }
            db.SaveChanges();

            if (!db.Music.Any())
            {
                db.Music.Add(new Music { Name = "Girls Just Wanna Have Fun", Description = "fun pop song", LibraryCode = "M001", Status = "available", Artist = "Cyndi Lauper", Year = 1983, Genre = "Pop" });
                db.Music.Add(new Music { Name = "Run the World (Girls)", Description = "girl power song", LibraryCode = "M002", Status = "available", Artist = "Beyonce", Year = 2011, Genre = "Pop" });
                db.Music.Add(new Music { Name = "Firework", Description = "song about believing in yourself", LibraryCode = "M003", Status = "available", Artist = "Katy Perry", Year = 2010, Genre = "Pop" });
                db.Music.Add(new Music { Name = "Roar", Description = "song about finding your courage", LibraryCode = "M004", Status = "borrowed", Artist = "Katy Perry", Year = 2013, Genre = "Pop" });
                db.SaveChanges();
            }

            if (!db.Toys.Any())
            {
                db.Toys.Add(new Toy { Name = "Barbie Doll", Description = "classic doll toy", LibraryCode = "T001", Status = "available", ToyType = "Doll", Age = 4 });
                db.Toys.Add(new Toy { Name = "Princess Castle Playset", Description = "castle toy with tiny princess figures", LibraryCode = "T002", Status = "available", ToyType = "Playset", Age = 5 });
                db.Toys.Add(new Toy { Name = "Frozen Elsa Dress", Description = "dress up costume for pretend play", LibraryCode = "T003", Status = "available", ToyType = "Costume", Age = 6 });
                db.Toys.Add(new Toy { Name = "Rainbow Unicorn Plush", Description = "soft toy unicorn", LibraryCode = "T004", Status = "destroy", ToyType = "Plush", Age = 3 });
                db.SaveChanges();
            }
        }

        private static void EnsureMultiBranchSchema(AppDbContext db)
        {
            var connection = db.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen)
            {
                connection.Open();
            }

            try
            {
                var bookColumns = new HashSet<string>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA table_info('Books');";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        bookColumns.Add(reader.GetString(1));
                    }
                }

                AddColumnIfMissing(connection, bookColumns, "CurrentBranchId", "\"CurrentBranchId\" INTEGER NULL");
                AddColumnIfMissing(connection, bookColumns, "HomeBranchId", "\"HomeBranchId\" INTEGER NULL");
                AddColumnIfMissing(connection, bookColumns, "IsInTransit", "\"IsInTransit\" INTEGER NOT NULL DEFAULT 0");

                Execute(connection, """
                    CREATE TABLE IF NOT EXISTS "Branches" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Branches" PRIMARY KEY AUTOINCREMENT,
                        "Name" TEXT NOT NULL,
                        "Code" TEXT NOT NULL,
                        "Address" TEXT NOT NULL,
                        "PhoneNumber" TEXT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Branches_Code" ON "Branches" ("Code");

                    CREATE TABLE IF NOT EXISTS "BranchTransfers" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_BranchTransfers" PRIMARY KEY AUTOINCREMENT,
                        "BookId" INTEGER NOT NULL,
                        "FromBranchId" INTEGER NOT NULL,
                        "ToBranchId" INTEGER NOT NULL,
                        "RequestedAt" TEXT NOT NULL,
                        "DispatchedAt" TEXT NULL,
                        "CompletedAt" TEXT NULL,
                        "Status" INTEGER NOT NULL,
                        "Notes" TEXT NULL,
                        "RequestedByStaffId" TEXT NULL,
                        CONSTRAINT "FK_BranchTransfers_Books_BookId" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE RESTRICT,
                        CONSTRAINT "FK_BranchTransfers_Branches_FromBranchId" FOREIGN KEY ("FromBranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT,
                        CONSTRAINT "FK_BranchTransfers_Branches_ToBranchId" FOREIGN KEY ("ToBranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT
                    );
                    CREATE INDEX IF NOT EXISTS "IX_BranchTransfers_BookId" ON "BranchTransfers" ("BookId");
                    CREATE INDEX IF NOT EXISTS "IX_BranchTransfers_FromBranchId" ON "BranchTransfers" ("FromBranchId");
                    CREATE INDEX IF NOT EXISTS "IX_BranchTransfers_ToBranchId" ON "BranchTransfers" ("ToBranchId");
                    CREATE INDEX IF NOT EXISTS "IX_Books_CurrentBranchId" ON "Books" ("CurrentBranchId");
                    CREATE INDEX IF NOT EXISTS "IX_Books_HomeBranchId" ON "Books" ("HomeBranchId");
                    """);
            }
            finally
            {
                if (!wasOpen)
                {
                    connection.Close();
                }
            }
        }

        // adds the reservation/notification tables plus the two new columns on
        // BorrowTransactions, for an existing db file that was made before this feature
        private static void EnsureNotificationSchema(AppDbContext db)
        {
            var connection = db.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen)
            {
                connection.Open();
            }

            try
            {
                var transColumns = new HashSet<string>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA table_info('BorrowTransactions');";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        transColumns.Add(reader.GetString(1));
                    }
                }

                if (!transColumns.Contains("DueSoonNotifiedAt"))
                {
                    Execute(connection, "ALTER TABLE \"BorrowTransactions\" ADD COLUMN \"DueSoonNotifiedAt\" TEXT NULL;");
                }
                if (!transColumns.Contains("OverdueNotifiedAt"))
                {
                    Execute(connection, "ALTER TABLE \"BorrowTransactions\" ADD COLUMN \"OverdueNotifiedAt\" TEXT NULL;");
                }

                Execute(connection, """
                    CREATE TABLE IF NOT EXISTS "Reservations" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Reservations" PRIMARY KEY AUTOINCREMENT,
                        "ItemType" TEXT NOT NULL,
                        "ItemId" INTEGER NOT NULL,
                        "ItemName" TEXT NULL,
                        "BorrowerId" INTEGER NOT NULL,
                        "BorrowerName" TEXT NULL,
                        "ReservedDate" TEXT NOT NULL,
                        "Status" TEXT NOT NULL,
                        "NotifiedDate" TEXT NULL
                    );
                    CREATE INDEX IF NOT EXISTS "IX_Reservations_Item" ON "Reservations" ("ItemType", "ItemId");

                    CREATE TABLE IF NOT EXISTS "Notifications" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY AUTOINCREMENT,
                        "BorrowerId" INTEGER NOT NULL,
                        "BorrowerName" TEXT NULL,
                        "Channel" TEXT NOT NULL,
                        "NotifyType" TEXT NOT NULL,
                        "Message" TEXT NOT NULL,
                        "SentDate" TEXT NOT NULL
                    );
                    """);
            }
            finally
            {
                if (!wasOpen)
                {
                    connection.Close();
                }
            }
        }

        private static void AddColumnIfMissing(System.Data.Common.DbConnection connection, HashSet<string> columns, string name, string definition)
        {
            if (columns.Contains(name))
            {
                return;
            }

            Execute(connection, $"ALTER TABLE \"Books\" ADD COLUMN {definition};");
        }

        private static void Execute(System.Data.Common.DbConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
