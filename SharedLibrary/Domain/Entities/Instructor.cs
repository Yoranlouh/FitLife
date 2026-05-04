using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class Instructor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = null!;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Specialization { get; set; }
        
        public int? PhotoId { get; set; }
        public Photo? Photo { get; set; }

        // Relaties
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
