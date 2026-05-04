namespace SharedLibrary.DTOs.Responses
{
    public class ReservationResponse
    {
        public int Id { get; set; }
        public DateTime ReservationDate { get; set; }
        public bool IsCancelled { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = null!;
        public int LessonId { get; set; }
        public string WorkoutName { get; set; } = null!;
        public DateTime LessonStartTime { get; set; }
    }
}
