using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.DTOs.Requests
{
    public class WorkoutCreateRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public TimeSpan Duration { get; set; }
    }
}
