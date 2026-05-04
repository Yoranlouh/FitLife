using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.DTOs.Requests
{
    public class ReservationCreateRequest
    {
        [Required]
        public int MemberId { get; set; }

        [Required]
        public int LessonId { get; set; }
    }
}
