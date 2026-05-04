using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class Location
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(200)]
        public string? Address { get; set; }

        [Required]
        public int Capacity { get; set; }

        // Relaties
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
