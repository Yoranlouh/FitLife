namespace SharedLibrary.DTOs.Responses
{
    public class SubscriptionStatusResponse
    {
        public int MemberId { get; set; }
        public string? SubscriptionName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool WillAutoRenew { get; set; }
        public decimal CurrentMonthlyPrice { get; set; }
    }

    public class PriceCalculationResponse
    {
        public decimal TotalPrice { get; set; }
        public decimal MonthlyPrice { get; set; }
        public string Description { get; set; } = null!;
    }
}
