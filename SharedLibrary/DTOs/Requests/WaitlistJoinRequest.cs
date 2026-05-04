using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.DTOs.Requests
{
    public class WaitlistJoinRequest
    {
        [Required]
        public int MemberId { get; set; }

        [Required]
        public int LessonId { get; set; }
    }
}
