using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class Member
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public DateTime JoinDate { get; set; }
        
        public string? Username { get; set; }
        public string? ProfileDescription { get; set; }
        public string? Status { get; set; } // bijv. "Active", "Inactive", "Pending"

        public int? PhotoId { get; set; }
        public Photo? Photo { get; set; }

        // Relaties
        public int? SubscriptionId { get; set; }
        public Subscription? Subscription { get; set; }

        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public bool IsAutoRenewEnabled { get; set; } = true;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();
    }
}
