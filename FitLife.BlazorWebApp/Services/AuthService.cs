using FitLife.BlazorWebApp.Models;
using MySqlConnector;
using System.Security.Cryptography;
using System.Text;

namespace FitLife.BlazorWebApp.Services;

// Service for handling user authentication in the Blazor admin panel.
// Verifies credentials against the database and manages the HTTP-only auth cookie
// that keeps the admin logged in across page navigations and browser refreshes.
// Only users with role 'employee' or 'instructor' can log in here.
public class AuthService : IAuthService
{
    private readonly IConfiguration          _configuration;
    private readonly ISessionService         _sessionService;
    private readonly IHttpContextAccessor    _httpContextAccessor;

    public AuthService(IConfiguration configuration,
                       ISessionService sessionService,
                       IHttpContextAccessor httpContextAccessor)
    {
        _configuration       = configuration;
        _sessionService      = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    // Reads the current session ID from the ".FitLife.Auth" cookie,
    // or falls back to the ASP.NET session ID when the cookie is absent.
    private string GetSessionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return string.Empty;

        // Prefer the explicit auth cookie over the generic session ID
        if (httpContext.Request.Cookies.TryGetValue(".FitLife.Auth", out var existingToken))
            return existingToken;

        // Ensure a session exists (creates one if needed)
        if (string.IsNullOrEmpty(httpContext.Session.Id))
            httpContext.Session.SetString("init", "true");

        return httpContext.Session.Id;
    }

    // Writes the ".FitLife.Auth" HttpOnly cookie so the session survives browser refreshes.
    // HttpOnly = not accessible to JavaScript (XSS protection).
    // MaxAge = 8 hours so admins aren't logged out mid-shift.
    private void SetAuthCookie(string sessionId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Append(".FitLife.Auth", sessionId, new CookieOptions
            {
                HttpOnly    = true,
                Secure      = httpContext.Request.IsHttps,
                SameSite    = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge      = TimeSpan.FromHours(8)
            });
        }
    }

    // Removes the auth cookie on logout.
    private void RemoveAuthCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
            httpContext.Response.Cookies.Delete(".FitLife.Auth");
    }

    // Looks up the user by email, verifies the SHA-256 password hash,
    // and on success creates an in-memory session + sets the auth cookie.
    // Returns (false, message, null) for any failure so the Razor page can display the error.
    public async Task<(bool Success, string Message, UserSessionDto? User)> LoginAsync(string email, string password)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "Database connection niet geconfigureerd.", null);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "Email en wachtwoord zijn verplicht.", null);

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Restrict access to employee/instructor — members log in via the MAUI app
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
                var userId       = reader.GetInt32("id");
                var userEmail    = reader.GetString("email");
                var passwordHash = reader.GetString("password_hash");
                var displayName  = reader.IsDBNull(reader.GetOrdinal("display_name"))
                                   ? email.Split('@')[0]
                                   : reader.GetString("display_name");
                var role = reader.GetString("role");

                // Passwords are stored as SHA-256 hex strings; also accept plaintext for legacy seeds
                string computedHash = ComputeSha256Hash(password);

                if (passwordHash == computedHash || passwordHash == password)
                {
                    var user = new UserSessionDto
                    {
                        UserId          = userId,
                        DisplayName     = displayName,
                        Email           = userEmail,
                        Role            = role,
                        IsAuthenticated = true
                    };

                    // Generate a random session ID, store the user object, set the cookie
                    var sessionId = Guid.NewGuid().ToString();
                    _sessionService.SetUserSession(sessionId, user);
                    SetAuthCookie(sessionId);

                    return (true, "Login succesvol!", user);
                }
            }

            return (false, "Ongeldige inloggegevens of geen toegang.", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het inloggen.", null);
        }
    }

    // Removes the in-memory session and the auth cookie, effectively logging the user out.
    public Task LogoutAsync()
    {
        var sessionId = GetSessionId();
        _sessionService.RemoveUserSession(sessionId);
        RemoveAuthCookie();
        return Task.CompletedTask;
    }

    // Reads the session ID from the cookie/session store and looks up the associated user.
    // Returns null when not logged in or when the session has expired.
    public Task<UserSessionDto?> GetCurrentUserAsync()
    {
        try
        {
            var sessionId = GetSessionId();
            var user      = _sessionService.GetUserSession(sessionId);
            return Task.FromResult(user);
        }
        catch
        {
            return Task.FromResult<UserSessionDto?>(null);
        }
    }

    // Computes the hex-encoded SHA-256 hash of a password string.
    // Used to verify against the hash stored in the database.
    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var builder  = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
            builder.Append(bytes[i].ToString("x2"));
        return builder.ToString();
    }
}
