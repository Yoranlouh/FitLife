using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.DTOs.Requests
{
    public class LessonCreateRequest
    {
        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public int MaxParticipants { get; set; }

        [Required]
        public int WorkoutId { get; set; }

        [Required]
        public int InstructorId { get; set; }

        [Required]
        public int LocationId { get; set; }

        // Herhaling
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; } // "Daily", "Weekly", "Monthly"
        public int? RecurrenceInterval { get; set; } = 1;
        public DateTime? RecurrenceEndDate { get; set; }
        public int? RecurrenceCount { get; set; }
    }
}