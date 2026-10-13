namespace Habibalib.Models
{
    // this is the parent class, book/music/toy will inherit from this
    public abstract class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string LibraryCode { get; set; }
        public string Status { get; set; } = "available"; // available, borrowed, damaged, destroy
    }
}
