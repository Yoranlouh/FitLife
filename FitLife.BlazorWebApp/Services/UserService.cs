using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for managing users in the database (admin functionality)
/// </summary>
public class UserService : IUserService
{
    private readonly IConfiguration _configuration;

    public UserService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
               ?? throw new InvalidOperationException("Database connection not configured.");
    }

    /// <summary>
    /// Retrieves all users, optionally filtered by role
    /// </summary>
    public async Task<List<MemberDto>> GetAllUsersAsync(string? roleFilter = null)
    {
        var users = new List<MemberDto>();
        
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        var sql = """
            SELECT
                u.id,
                u.display_name,
                u.email,
                u.role,
                u.photo_url,
                u.created_at,
                (SELECT COUNT(*) FROM reservations r WHERE r.member_id = u.id AND r.is_cancelled = 0) AS total_reservations,
                (SELECT MAX(r.reservation_date) FROM reservations r WHERE r.member_id = u.id) AS last_activity
            FROM users u
            WHERE 1=1
            """;

        if (!string.IsNullOrEmpty(roleFilter))
        {
            sql += " AND u.role = @roleFilter";
        }

        sql += " ORDER BY u.display_name";

        await using var command = new MySqlCommand(sql, connection);
        
        if (!string.IsNullOrEmpty(roleFilter))
            command.Parameters.AddWithValue("@roleFilter", roleFilter);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new MemberDto
            {
                Id = reader.GetInt32("id"),
                DisplayName = reader.IsDBNull(reader.GetOrdinal("display_name")) ? "Onbekend" : reader.GetString("display_name"),
                Email = reader.GetString("email"),
                Role = reader.GetString("role"),
                PhotoUrl = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? null : reader.GetString("photo_url"),
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime("created_at"),
                TotalReservations = reader.GetInt32("total_reservations"),
                LastActivity = reader.IsDBNull(reader.GetOrdinal("last_activity")) ? null : reader.GetDateTime("last_activity")
            });
        }

        return users;
    }

    /// <summary>
    /// Gets a single user by ID
    /// </summary>
    public async Task<MemberDto?> GetUserByIdAsync(int userId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                u.id,
                u.display_name,
                u.email,
                u.role,
                u.photo_url,
                u.created_at,
                (SELECT COUNT(*) FROM reservations r WHERE r.member_id = u.id AND r.is_cancelled = 0) AS total_reservations,
                (SELECT MAX(r.reservation_date) FROM reservations r WHERE r.member_id = u.id) AS last_activity
            FROM users u
            WHERE u.id = @userId
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new MemberDto
            {
                Id = reader.GetInt32("id"),
                DisplayName = reader.IsDBNull(reader.GetOrdinal("display_name")) ? "Onbekend" : reader.GetString("display_name"),
                Email = reader.GetString("email"),
                Role = reader.GetString("role"),
                PhotoUrl = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? null : reader.GetString("photo_url"),
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime("created_at"),
                TotalReservations = reader.GetInt32("total_reservations"),
                LastActivity = reader.IsDBNull(reader.GetOrdinal("last_activity")) ? null : reader.GetDateTime("last_activity")
            };
        }

        return null;
    }

    /// <summary>
    /// Updates a user's role
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserRoleAsync(int userId, string newRole)
    {
        var validRoles = new[] { "member", "instructor", "admin" };
        if (!validRoles.Contains(newRole))
        {
            return (false, "Ongeldige rol opgegeven.");
        }

        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = "UPDATE users SET role = @role WHERE id = @userId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@role", newRole);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Gebruikersrol succesvol bijgewerkt.") 
                : (false, "Gebruiker niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user role: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van de gebruikersrol.");
        }
    }
}