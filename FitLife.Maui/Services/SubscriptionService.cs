using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

/// <summary>
/// DTO for subscription plan data from API
/// </summary>
public class SubscriptionPlanDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("monthlyPrice")]
    public decimal MonthlyPrice { get; set; }

    [JsonPropertyName("yearlyPrice")]
    public decimal YearlyPrice { get; set; }

    [JsonPropertyName("credits")]
    public int Credits { get; set; }

    [JsonPropertyName("isUnlimited")]
    public bool IsUnlimited { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO for subscription change request
/// </summary>
public class SubscriptionChangeRequestDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("newSubscriptionType")]
    public string NewSubscriptionType { get; set; } = string.Empty;

    [JsonPropertyName("isYearly")]
    public bool IsYearly { get; set; }
}

/// <summary>
/// DTO for subscription change response
/// </summary>
public class SubscriptionChangeResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("effectiveDate")]
    public string? EffectiveDate { get; set; }

    [JsonPropertyName("newSubscriptionType")]
    public string? NewSubscriptionType { get; set; }

    [JsonPropertyName("billingCycle")]
    public string? BillingCycle { get; set; }
}

/// <summary>
/// DTO for subscription status including pending changes
/// </summary>
public class SubscriptionStatusDto
{
    [JsonPropertyName("currentSubscriptionType")]
    public string? CurrentSubscriptionType { get; set; }

    [JsonPropertyName("renewalDate")]
    public string? RenewalDate { get; set; }

    [JsonPropertyName("pendingSubscriptionChange")]
    public string? PendingSubscriptionChange { get; set; }

    [JsonPropertyName("pendingBillingCycle")]
    public string? PendingBillingCycle { get; set; }

    [JsonPropertyName("credits")]
    public int Credits { get; set; }
}

/// <summary>
/// Interface for subscription service
/// </summary>
public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionPlanDto>> GetAvailablePlansAsync();
    Task<SubscriptionChangeResponseDto> RequestSubscriptionChangeAsync(int userId, string newSubscriptionType, bool isYearly);
    Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(int userId);
    Task<SubscriptionChangeResponseDto> CancelSubscriptionAsync(int userId);
    Task<SubscriptionChangeResponseDto> ChangeBillingCycleAsync(int userId, bool isYearly);
}

/// <summary>
/// Service that manages subscription operations by communicating with the API
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly HttpClient _httpClient;

    public SubscriptionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get all available subscription plans with pricing information
    /// </summary>
    public async Task<IEnumerable<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[SubscriptionService] Fetching available plans");
            var response = await _httpClient.GetAsync("subscriptions/plans");

            if (response.IsSuccessStatusCode)
            {
                var plans = await response.Content.ReadFromJsonAsync<IEnumerable<SubscriptionPlanDto>>();
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Retrieved {plans?.Count() ?? 0} plans");
                return plans ?? Enumerable.Empty<SubscriptionPlanDto>();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Failed to fetch plans. Status: {response.StatusCode}");
                return Enumerable.Empty<SubscriptionPlanDto>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Error fetching plans: {ex.Message}");
            return Enumerable.Empty<SubscriptionPlanDto>();
        }
    }

    /// <summary>
    /// Request a subscription change that will be applied on the next renewal date
    /// </summary>
    public async Task<SubscriptionChangeResponseDto> RequestSubscriptionChangeAsync(int userId, string newSubscriptionType, bool isYearly)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Requesting subscription change for user {userId} to {newSubscriptionType} (yearly: {isYearly})");

            var request = new SubscriptionChangeRequestDto
            {
                UserId = userId,
                NewSubscriptionType = newSubscriptionType,
                IsYearly = isYearly
            };

            var response = await _httpClient.PostAsJsonAsync("subscriptions/change", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubscriptionChangeResponseDto>();
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Change request result: {result?.Success}, Message: {result?.Message}");
                return result ?? new SubscriptionChangeResponseDto
                {
                    Success = false,
                    Message = "Ongeldig antwoord van de server"
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Failed to request subscription change. Status: {response.StatusCode}");
                return new SubscriptionChangeResponseDto
                {
                    Success = false,
                    Message = "Er is een fout opgetreden bij het wijzigen van je abonnement"
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Error requesting subscription change: {ex.Message}");
            return new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Netwerkfout. Probeer het later opnieuw."
            };
        }
    }

    /// <summary>
    /// Get the current subscription status including any pending changes
    /// </summary>
    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(int userId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Fetching subscription status for user {userId}");
            var response = await _httpClient.GetAsync($"subscriptions/status/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<SubscriptionStatusDto>();
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Status retrieved: {status?.CurrentSubscriptionType}, Pending: {status?.PendingSubscriptionChange}");
                return status;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Failed to fetch status. Status: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Error fetching status: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Cancel the user's subscription (will be effective on renewal date)
    /// </summary>
    public async Task<SubscriptionChangeResponseDto> CancelSubscriptionAsync(int userId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Cancelling subscription for user {userId}");

            var request = new { UserId = userId };
            var response = await _httpClient.PostAsJsonAsync("subscriptions/cancel", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubscriptionChangeResponseDto>();
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Cancel result: {result?.Success}, Message: {result?.Message}");
                return result ?? new SubscriptionChangeResponseDto
                {
                    Success = false,
                    Message = "Ongeldig antwoord van de server"
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Failed to cancel subscription. Status: {response.StatusCode}");
                return new SubscriptionChangeResponseDto
                {
                    Success = false,
                    Message = "Er is een fout opgetreden bij het annuleren van je abonnement"
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Error cancelling subscription: {ex.Message}");
            return new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Netwerkfout. Probeer het later opnieuw."
            };
        }
    }

    /// <summary>
    /// Change the billing cycle (monthly/yearly) for the current subscription
    /// </summary>
    public async Task<SubscriptionChangeResponseDto> ChangeBillingCycleAsync(int userId, bool isYearly)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Changing billing cycle for user {userId} to {(isYearly ? "yearly" : "monthly")}");

            var request = new { UserId = userId, IsYearly = isYearly };
            var response = await _httpClient.PostAsJsonAsync("subscriptions/change-billing", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubscriptionChangeResponseDto>();
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Billing cycle change result: {result?.Success}, Message: {result?.Message}");
                return result ?? new SubscriptionChangeResponseDto
                {
                    Success = false,
                    Message = "Ongeldig antwoord van de server"
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Failed to change billing cycle. Status: {response.StatusCode}");
                return new SubscriptionChangeResponseDto
                {
                    Success = false,
                    Message = "Er is een fout opgetreden bij het wijzigen van je factureringsperiode"
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionService] Error changing billing cycle: {ex.Message}");
            return new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Netwerkfout. Probeer het later opnieuw."
            };
        }
    }
}
