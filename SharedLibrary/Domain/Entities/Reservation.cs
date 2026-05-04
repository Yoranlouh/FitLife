using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        public bool IsCancelled { get; set; }

        // Relaties
        [Required]
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;

        [Required]
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        
        // Optioneel: Koppeling aan een specifieke spinningbike
        public int? SpinningBikeId { get; set; }
        public SpinningBike? SpinningBike { get; set; }
    }
}
