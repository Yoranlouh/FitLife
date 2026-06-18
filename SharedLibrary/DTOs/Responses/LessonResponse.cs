namespace SharedLibrary.DTOs.Responses
{
    // DTO representing a single lesson row, returned by the API and consumed by
    // both the MAUI app and (indirectly) the Blazor admin panel.
    // Shared via the SharedLibrary project so both clients use the same model.
    public class LessonResponse
    {
        public int      Id              { get; set; }
        public DateTime StartTime       { get; set; }
        public DateTime EndTime         { get; set; }

        // Maximum number of participants (resolved from capacity_override → workout default → location capacity)
        public int      MaxParticipants { get; set; }

        public int      WorkoutId       { get; set; }
        public string   WorkoutName     { get; set; } = null!;

        public int      InstructorId    { get; set; }
        public string   InstructorName  { get; set; } = null!;

        public int      LocationId      { get; set; }
        public string   LocationName    { get; set; } = null!;

        // Recurring lesson support (not currently used in the active codebase)
        public bool     IsRecurring          { get; set; }
        public string?  RecurrencePattern    { get; set; }
        public int?     RecurrenceInterval   { get; set; }
        public DateTime? RecurrenceEndDate   { get; set; }
        public int?     RecurrenceCount      { get; set; }
        public int?     ParentLessonId       { get; set; }

        // Live counts — computed at query time via subqueries
        public int      CurrentParticipantCount { get; set; }
        public int      WaitlistCount           { get; set; }

        // True when the current user already has an active reservation for this lesson
        public bool     IsBooked     { get; set; }

        // True when the current user is on the waitlist for this lesson
        public bool     IsOnWaitlist { get; set; }

        // Hex colour string (e.g. "#5B6636") used to tint lesson cards in the schedule UI
        public string   WorkoutColor { get; set; } = "#5B6636";
    }
}
