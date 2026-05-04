using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.DTOs.Requests
{
    public class MemberCreateRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = null!;

        [StringLength(50)]
        public string? Username { get; set; }

        public string? ProfileDescription { get; set; }

        public int? SubscriptionId { get; set; }
    }
}
