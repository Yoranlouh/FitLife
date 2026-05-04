using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.DTOs.Requests
{
    public class MemberUpdateRequest
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Username { get; set; }

        public string? ProfileDescription { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public int? SubscriptionId { get; set; }
    }
}
