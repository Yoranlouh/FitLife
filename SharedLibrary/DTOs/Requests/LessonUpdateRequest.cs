namespace SharedLibrary.DTOs.Requests
{
    public class LessonUpdateRequest
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxParticipants { get; set; }
        public int WorkoutId { get; set; }
        public int InstructorId { get; set; }
        public int LocationId { get; set; }
        
        // Meestal update je een specifieke les, niet de hele reeks via deze DTO
        // Maar we kunnen herhalingsvelden toevoegen als dat nodig is.
    }
}