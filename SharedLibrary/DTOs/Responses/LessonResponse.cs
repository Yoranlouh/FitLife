namespace SharedLibrary.DTOs.Responses
{
    public class LessonResponse
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxParticipants { get; set; }
        public int WorkoutId { get; set; }
        public string WorkoutName { get; set; } = null!;
        public int InstructorId { get; set; }
        public string InstructorName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationName { get; set; } = null!;
        
        // Herhaling
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
        public int? RecurrenceCount { get; set; }
        public int? ParentLessonId { get; set; }

        public int CurrentParticipantCount { get; set; }
        public int WaitlistCount { get; set; }
    }
}