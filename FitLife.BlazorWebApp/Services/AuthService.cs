using FitLife.BlazorWebApp.Models;
using MySqlConnector;
using System.Security.Cryptography;
using System.Text;
using Blazored.LocalStorage;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for handling user authentication against the MySQL database
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILocalStorageService _localStorage;
    private const string UserSessionKey = "fitlife_admin_session";

    public AuthService(IConfiguration configuration, ILocalStorageService localStorage)
    {
        _configuration = configuration;
        _localStorage = localStorage;
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

            // Query to get user by email - only admin and instructor roles allowed
            const string sql = """
                SELECT id, email, password_hash, display_name, role
                FROM users
                WHERE email = @email AND role IN ('admin', 'instructor')
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

                    // Store session in local storage
                    await _localStorage.SetItemAsync(UserSessionKey, user);

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

    /// <summary>
    /// Removes the user session from local storage
    /// </summary>
    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(UserSessionKey);
    }

    /// <summary>
    /// Retrieves the current user session from local storage
    /// </summary>
    public async Task<UserSessionDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<UserSessionDto>(UserSessionKey);
        }
        catch
        {
            return null;
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