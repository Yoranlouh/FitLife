namespace SharedLibrary.DTOs.Responses
{
    public class InstructorResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Email { get; set; }
        public string? Specialization { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
