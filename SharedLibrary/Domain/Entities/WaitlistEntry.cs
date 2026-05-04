using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class WaitlistEntry
    {
        public int Id { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        public int Position { get; set; }

        // Relaties
        [Required]
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;

        [Required]
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
    }
}
