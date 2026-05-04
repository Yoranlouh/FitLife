namespace SharedLibrary.DTOs.Requests
{
    public class SubscriptionUpgradeRequest
    {
        public int NewSubscriptionId { get; set; }
    }

    public class SubscriptionRenewRequest
    {
        public int? DurationInMonths { get; set; }
    }
}
