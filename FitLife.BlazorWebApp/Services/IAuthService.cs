namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    Task<(bool Success, string Message, UserSessionDto? User)> LoginAsync(string email, string password);
    
    /// <summary>
    /// Logs out the current user
    /// </summary>
    Task LogoutAsync();
    
    /// <summary>
    /// Gets the current authenticated user session
    /// </summary>
    Task<UserSessionDto?> GetCurrentUserAsync();
}