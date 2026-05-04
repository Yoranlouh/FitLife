namespace SharedLibrary.DTOs.Responses
{
    public class WaitlistResponse
    {
        public int Id { get; set; }
        public DateTime RequestDate { get; set; }
        public int Position { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = null!;
        public int LessonId { get; set; }
        public string WorkoutName { get; set; } = null!;
        public DateTime LessonStartTime { get; set; }
    }
}
