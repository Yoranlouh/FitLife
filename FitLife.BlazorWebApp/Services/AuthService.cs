using FitLife.BlazorWebApp.Models;
using MySqlConnector;
using System.Security.Cryptography;
using System.Text;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for handling user authentication against the MySQL database
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ISessionService _sessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(IConfiguration configuration, ISessionService sessionService, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetSessionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return string.Empty;

        // Try to get existing auth token from cookie
        if (httpContext.Request.Cookies.TryGetValue(".FitLife.Auth", out var existingToken))
        {
            return existingToken;
        }

        // Use or create a session ID as fallback
        if (string.IsNullOrEmpty(httpContext.Session.Id))
        {
            // Force session to be created
            httpContext.Session.SetString("init", "true");
        }

        return httpContext.Session.Id;
    }

    private void SetAuthCookie(string sessionId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Append(".FitLife.Auth", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromHours(8)
            });
        }
    }

    private void RemoveAuthCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Delete(".FitLife.Auth");
        }
    }

    /// <summary>
    /// Authenticates user credentials against the database
    /// Only allows admin and instructor roles to login
    /// </summary>
    public async Task<(bool Success, string Message, UserSessionDto? User)> LoginAsync(string email, string password)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, "Database connection niet geconfigureerd.", null);
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Email en wachtwoord zijn verplicht.", null);
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Query to get user by email - only employee and instructor roles allowed
            const string sql = """
                SELECT id, email, password_hash, display_name, role
                FROM users
                WHERE email = @email AND role IN ('employee', 'instructor')
                LIMIT 1;
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@email", email);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var userId = reader.GetInt32("id");
                var userEmail = reader.GetString("email");
                var passwordHash = reader.GetString("password_hash");
                var displayName = reader.IsDBNull(reader.GetOrdinal("display_name"))
                    ? email.Split('@')[0]
                    : reader.GetString("display_name");
                var role = reader.GetString("role");

                // Verify password using SHA256 hash (matching existing API logic)
                string computedHash = ComputeSha256Hash(password);

                if (passwordHash == computedHash || passwordHash == password)
                {
                    var user = new UserSessionDto
                    {
                        UserId = userId,
                        DisplayName = displayName,
                        Email = userEmail,
                        Role = role,
                        IsAuthenticated = true
                    };

                    // Generate a unique session ID for this login
                    var sessionId = Guid.NewGuid().ToString();
                    
                    // Store session in memory
                    _sessionService.SetUserSession(sessionId, user);
                    
                    // Set authentication cookie
                    SetAuthCookie(sessionId);

                    return (true, "Login succesvol!", user);
                }
            }

            return (false, "Ongeldige inloggegevens of geen toegang.", null);
        }
        catch (Exception ex)
        {
            return (false, "Er is een fout opgetreden bij het inloggen.", null);
        }
    }

    /// <summary>
    /// Removes the user session from memory
    /// </summary>
    public Task LogoutAsync()
    {
        var sessionId = GetSessionId();
        _sessionService.RemoveUserSession(sessionId);
        RemoveAuthCookie();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves the current user session from memory
    /// </summary>
    public Task<UserSessionDto?> GetCurrentUserAsync()
    {
        try
        {
            var sessionId = GetSessionId();
            var user = _sessionService.GetUserSession(sessionId);
            return Task.FromResult(user);
        }
        catch
        {
            return Task.FromResult<UserSessionDto?>(null);
        }
    }

    /// <summary>
    /// Computes SHA256 hash of a password string
    /// </summary>
    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}