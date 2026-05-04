namespace SharedLibrary.DTOs.Responses
{
    public class MemberResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Username { get; set; }
        public string? ProfileDescription { get; set; }
        public string? Status { get; set; }
        public DateTime JoinDate { get; set; }
        public string? PhotoUrl { get; set; }
        public int? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }
    }
}
