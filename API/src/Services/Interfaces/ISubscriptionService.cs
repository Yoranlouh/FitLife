using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<SubscriptionStatusResponse> GetStatusAsync(int memberId);
        Task<SubscriptionStatusResponse> UpgradeAsync(int memberId, SubscriptionUpgradeRequest request);
        Task<SubscriptionStatusResponse> RenewAsync(int memberId, SubscriptionRenewRequest request);
        Task<PriceCalculationResponse> CalculatePriceAsync(int subscriptionId, int durationInMonths);
        Task ProcessNotificationsAsync();
    }
}
