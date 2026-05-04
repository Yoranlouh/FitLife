using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.DTOs.Requests
{
    public class InstructorUpdateRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = null!;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Specialization { get; set; }
    }
}
