using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public int MaxParticipants { get; set; }

        // Relaties
        [Required]
        public int WorkoutId { get; set; }
        public Workout Workout { get; set; } = null!;

        [Required]
        public int InstructorId { get; set; }
        public Instructor Instructor { get; set; } = null!;

        [Required]
        public int LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();
        public ICollection<SpinningBike> SpinningBikes { get; set; } = new List<SpinningBike>();

        // Herhalingslogica
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; } // "Daily", "Weekly", "Monthly"
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
        public int? RecurrenceCount { get; set; }
        public int? ParentLessonId { get; set; }
        public Lesson? ParentLesson { get; set; }
    }
}
