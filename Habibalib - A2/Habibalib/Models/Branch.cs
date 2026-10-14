using System.ComponentModel.DataAnnotations;

namespace Habibalib.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; }

        [Required, StringLength(200)]
        public string Address { get; set; }

        [Phone, StringLength(30)]
        public string PhoneNumber { get; set; }

        public ICollection<Book> CurrentBooks { get; set; } = new List<Book>();
        public ICollection<Book> HomeBooks { get; set; } = new List<Book>();
    }
}
