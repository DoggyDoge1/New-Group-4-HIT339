using System.Linq;
using Habibalib.Models;

namespace Habibalib.Data
{
    public static class SeedData
    {
        public static void Run(AppDbContext db)
        {
            db.Database.EnsureCreated();

            if (!db.Users.Any())
            {
                db.Users.Add(new User { Email = "admin@gmail.com", Password = "pwdadmin@999", Role = "Admin" });
                db.Users.Add(new User { Email = "recept@gmail.com", Password = "pwdrecept@999", Role = "Reception" });
                db.Users.Add(new User { Email = "manager@gmail.com", Password = "pwd@999", Role = "Manager" });
                db.SaveChanges();
            }

            if (!db.Books.Any())
            {
                db.Books.Add(new Book { Name = "Anne of Green Gables", Description = "story of a red hair girl called anne", LibraryCode = "B001", Status = "available", Author = "L.M. Montgomery", Genre = "Fiction", Isbn = "111-111" });
                db.Books.Add(new Book { Name = "Matilda", Description = "a girl with magic powers", LibraryCode = "B002", Status = "available", Author = "Roald Dahl", Genre = "Fantasy", Isbn = "222-222" });
                db.Books.Add(new Book { Name = "Little Women", Description = "story about 4 sisters growing up", LibraryCode = "B003", Status = "available", Author = "Louisa May Alcott", Genre = "Fiction", Isbn = "333-333" });
                db.Books.Add(new Book { Name = "Pippi Longstocking", Description = "strong and brave girl story", LibraryCode = "B004", Status = "damaged", Author = "Astrid Lindgren", Genre = "Adventure", Isbn = "444-444" });
                db.SaveChanges();
            }

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
    }
}
