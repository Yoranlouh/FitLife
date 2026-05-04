namespace SharedLibrary.DTOs.Responses
{
    public class WorkoutResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
