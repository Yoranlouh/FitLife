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

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
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
    int? CurrentUserId { get; }
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
    /// Get the current user's ID
    /// </summary>
    public int? CurrentUserId => _currentUser?.UserId;

    /// <summary>
    /// Attempt to login with email and password
    /// Returns a LoginResponse with success status and user details if successful
    /// </summary>
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        try
        {
            // Create login request
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            // Send POST request to API
            var response = await _httpClient.PostAsJsonAsync("auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                // Parse response
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse != null && loginResponse.Success)
                {
                    // Store current user information
                    _currentUser = loginResponse;
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
