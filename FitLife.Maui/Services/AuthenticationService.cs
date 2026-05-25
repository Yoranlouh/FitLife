using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

/// <summary>
/// DTO for login request sent to the API
/// </summary>
public class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO for login response received from the API
/// </summary>
public class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("credits")]
    public int? Credits { get; set; }

    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    [JsonPropertyName("subscriptionRenewalDate")]
    public string? SubscriptionRenewalDate { get; set; }
}

/// <summary>
/// Interface for authentication service
/// </summary>
public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(string email, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    string? CurrentUserName { get; }
    string? CurrentUserPhotoUrl { get; }
    int? CurrentUserId { get; }
    int? CurrentUserCredits { get; }
    string? CurrentUserSubscriptionType { get; }
    string? CurrentUserSubscriptionRenewalDate { get; }
}

/// <summary>
/// Service that handles user authentication by communicating with the API
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private LoginResponse? _currentUser;

    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Check if a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _currentUser != null;

    /// <summary>
    /// Get the current user's display name
    /// </summary>
    public string? CurrentUserName => _currentUser?.DisplayName;

    /// <summary>
    /// Get the current user's profile photo URL
    /// </summary>
    public string? CurrentUserPhotoUrl => _currentUser?.PhotoUrl;

    /// <summary>
    /// Get the current user's ID
    /// </summary>
    public int? CurrentUserId => _currentUser?.UserId;

    /// <summary>
    /// Get the current user's remaining credits
    /// </summary>
    public int? CurrentUserCredits => _currentUser?.Credits;

    /// <summary>
    /// Get the current user's subscription type
    /// </summary>
    public string? CurrentUserSubscriptionType => _currentUser?.SubscriptionType;

    /// <summary>
    /// Get the current user's subscription renewal date
    /// </summary>
    public string? CurrentUserSubscriptionRenewalDate => _currentUser?.SubscriptionRenewalDate;

    /// <summary>
    /// Attempt to login with email and password
    /// Returns a LoginResponse with success status and user details if successful
    /// </summary>
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] LoginAsync called for email: {email}");
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] HttpClient BaseAddress: {_httpClient.BaseAddress}");
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] AuthenticationService instance: {this.GetHashCode()}");

            // Create login request
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            // Send POST request to API
            var response = await _httpClient.PostAsJsonAsync("auth/login", loginRequest);
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] API Response Status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[AuthenticationService] API Response Body: {responseContent}");

                // Parse response - need to reset stream position
                response = await _httpClient.PostAsJsonAsync("auth/login", loginRequest);
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse != null && loginResponse.Success)
                {
                    // Store current user information
                    _currentUser = loginResponse;
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService] Login successful!");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   UserId: {loginResponse.UserId}");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   DisplayName: '{loginResponse.DisplayName}'");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   Email: {loginResponse.Email}");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   Role: {loginResponse.Role}");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   Credits: {loginResponse.Credits}");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   SubscriptionType: '{loginResponse.SubscriptionType}'");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   _currentUser is now set: {_currentUser != null}");
                    System.Diagnostics.Debug.WriteLine($"[AuthenticationService]   IsAuthenticated: {IsAuthenticated}");
                    return loginResponse;
                }

                return loginResponse ?? new LoginResponse
                {
                    Success = false,
                    Message = "Invalid response from server"
                };
            }
            else
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Login failed. Please check your credentials."
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during login: {ex.Message}");
            return new LoginResponse
            {
                Success = false,
                Message = "Network error. Please try again."
            };
        }
    }

    /// <summary>
    /// Logout the current user
    /// </summary>
    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }
}
