namespace SharedLibrary.Domain.Entities
{
    public class User
    {
        public int Id { get; private set; } // EF kan private set
        public string Name { get; private set; } = null!;
        public string? PhotoId { get; set; }
        // Foreign key rule
        public Photo? Photo { get; set; } 

        // EF Core constructor
        private User()
        {
        }

        public User(string name)
        {
            Name = name;
        }
    }
}