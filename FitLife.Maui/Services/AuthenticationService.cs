using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

// Request body sent to POST /auth/login
public class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

// Response received from POST /auth/login.
// When Success = true, all user fields are populated.
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

    // Role values: "member", "instructor", or "admin"
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("credits")]
    public int? Credits { get; set; }

    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    [JsonPropertyName("subscriptionRenewalDate")]
    public string? SubscriptionRenewalDate { get; set; }
}

// Contract for the authentication service.
// Exposes the current session state as read-only properties and
// provides Login/Logout/UpdatePhotoUrl operations.
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
    string? CurrentUserRole { get; }
    bool IsAdmin { get; }
    bool IsInstructor { get; }
    bool IsMember { get; }

    // Updates the cached photo URL after a successful upload.
    // Without this, LoadUserData() would reset the profile picture back to the
    // login-time URL every time the profile page re-appears.
    void UpdatePhotoUrl(string? photoUrl);
}

// Singleton implementation that keeps the logged-in user's data in memory.
// Registered as Singleton so the session persists across all page navigations.
public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;

    // Holds the full login response while the user is logged in; null when logged out
    private LoginResponse? _currentUser;

    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // True when a user is currently logged in
    public bool IsAuthenticated => _currentUser != null;

    public string? CurrentUserName               => _currentUser?.DisplayName;
    public string? CurrentUserPhotoUrl           => _currentUser?.PhotoUrl;
    public int?    CurrentUserId                 => _currentUser?.UserId;
    public int?    CurrentUserCredits            => _currentUser?.Credits;
    public string? CurrentUserSubscriptionType   => _currentUser?.SubscriptionType;
    public string? CurrentUserSubscriptionRenewalDate => _currentUser?.SubscriptionRenewalDate;
    public string? CurrentUserRole               => _currentUser?.Role;

    // Role helpers used by the app to show/hide role-specific UI elements
    public bool IsAdmin      => string.Equals(_currentUser?.Role, "admin",       StringComparison.OrdinalIgnoreCase);
    public bool IsInstructor => string.Equals(_currentUser?.Role, "instructor",  StringComparison.OrdinalIgnoreCase);
    public bool IsMember     => !IsAdmin && !IsInstructor;

    // Sends the login credentials to POST /auth/login.
    // On success, stores the response in _currentUser so all session properties become available.
    // Note: currently calls the API twice due to a stream-reading workaround; can be simplified.
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] LoginAsync called for email: {email}");
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] HttpClient BaseAddress: {_httpClient.BaseAddress}");
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] AuthenticationService instance: {this.GetHashCode()}");

            var loginRequest = new LoginRequest { Email = email, Password = password };

            var response = await _httpClient.PostAsJsonAsync("auth/login", loginRequest);
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] API Response Status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[AuthenticationService] API Response Body: {responseContent}");

                // Re-send the request to read the JSON (stream was already consumed above)
                response = await _httpClient.PostAsJsonAsync("auth/login", loginRequest);
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse != null && loginResponse.Success)
                {
                    _currentUser = loginResponse;  // cache session data
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

    // Mutates the cached photo URL so subsequent reads of CurrentUserPhotoUrl return the new value.
    // Called immediately after a successful photo upload in ProfileViewModel.
    public void UpdatePhotoUrl(string? photoUrl)
    {
        if (_currentUser != null)
            _currentUser.PhotoUrl = photoUrl;
    }

    // Clears the session so IsAuthenticated returns false and all CurrentUser* properties return null.
    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }
}
