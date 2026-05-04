using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class Workout
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public TimeSpan Duration { get; set; }

        // Relaties
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
