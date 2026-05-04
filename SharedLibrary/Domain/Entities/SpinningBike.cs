using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class SpinningBike
    {
        public int Id { get; set; }

        [Required]
        public int BikeNumber { get; set; }

        public bool IsMaintenance { get; set; }

        // Relaties
        [Required]
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public Reservation? Reservation { get; set; }
    }
}
