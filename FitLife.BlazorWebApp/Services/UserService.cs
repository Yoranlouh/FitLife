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
                u.subscription_type,
                u.subscription_renewal_date,
                u.subscription_paused,
                (SELECT COUNT(*) FROM reservations r WHERE r.member_id = u.id AND r.is_cancelled = 0) AS total_reservations,
                (SELECT MAX(r.reservation_date) FROM reservations r WHERE r.member_id = u.id) AS last_activity
            FROM users u
            WHERE 1=1
            """;

        if (!string.IsNullOrEmpty(roleFilter))
        {
            sql += " AND u.role = @roleFilter";
        }

        sql += " ORDER BY u.created_at DESC";

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
                LastActivity = reader.IsDBNull(reader.GetOrdinal("last_activity")) ? null : reader.GetDateTime("last_activity"),
                SubscriptionType = reader.IsDBNull(reader.GetOrdinal("subscription_type")) ? null : reader.GetString("subscription_type"),
                SubscriptionRenewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date")) ? null : reader.GetDateTime("subscription_renewal_date"),
                SubscriptionPaused = !reader.IsDBNull(reader.GetOrdinal("subscription_paused")) && reader.GetBoolean("subscription_paused")
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
                u.subscription_type,
                u.subscription_renewal_date,
                u.subscription_paused,
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
                LastActivity = reader.IsDBNull(reader.GetOrdinal("last_activity")) ? null : reader.GetDateTime("last_activity"),
                SubscriptionType = reader.IsDBNull(reader.GetOrdinal("subscription_type")) ? null : reader.GetString("subscription_type"),
                SubscriptionRenewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date")) ? null : reader.GetDateTime("subscription_renewal_date"),
                SubscriptionPaused = !reader.IsDBNull(reader.GetOrdinal("subscription_paused")) && reader.GetBoolean("subscription_paused")
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

    public async Task<(bool Success, string Message)> DeleteUserAsync(int userId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = "DELETE FROM users WHERE id = @userId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true, "Lid succesvol verwijderd.")
                : (false, "Gebruiker niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting user: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van het lid.");
        }
    }

    public async Task<(bool Success, string Message)> PauseSubscriptionAsync(int userId, bool pause)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = "UPDATE users SET subscription_paused = @paused WHERE id = @userId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@paused", pause ? 1 : 0);
            command.Parameters.AddWithValue("@userId", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true, pause ? "Abonnement gepauzeerd." : "Abonnement hervat.")
                : (false, "Gebruiker niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error pausing subscription: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het pauzeren van het abonnement.");
        }
    }

    public async Task<(bool Success, string Message)> CreateMemberAsync(AdminCreateMemberDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return (false, "Wachtwoorden komen niet overeen.");

        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            await using (var check = new MySqlCommand("SELECT COUNT(*) FROM users WHERE email = @email", connection))
            {
                check.Parameters.AddWithValue("@email", dto.Email);
                if (Convert.ToInt64(await check.ExecuteScalarAsync()) > 0)
                    return (false, "Dit e-mailadres is al in gebruik.");
            }

            var credits = dto.SubscriptionPlan switch
            {
                "Rookie"       => 9,
                "Intermediate" => 13,
                "Advanced"     => 999,
                _              => 0
            };

            var renewalDate = dto.IsYearly
                ? dto.StartDate.AddYears(1)
                : dto.StartDate.AddMonths(1);

            var displayName = $"{dto.FirstName} {dto.LastName}".Trim();

            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.Password));
            var passwordHash = string.Concat(hashBytes.Select(b => b.ToString("x2")));

            const string sql = """
                INSERT INTO users
                    (email, password_hash, display_name, role, credits, subscription_type, subscription_renewal_date)
                VALUES
                    (@email, @passwordHash, @displayName, 'member', @credits, @subscriptionType, @renewalDate)
                """;

            await using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@email",            dto.Email);
            cmd.Parameters.AddWithValue("@passwordHash",     passwordHash);
            cmd.Parameters.AddWithValue("@displayName",      displayName);
            cmd.Parameters.AddWithValue("@credits",          credits);
            cmd.Parameters.AddWithValue("@subscriptionType", dto.SubscriptionPlan);
            cmd.Parameters.AddWithValue("@renewalDate",      renewalDate.Date);

            await cmd.ExecuteNonQueryAsync();
            return (true, $"Lid {displayName} succesvol aangemaakt.");
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            return (false, "Dit e-mailadres is al in gebruik.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating member: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van het lid.");
        }
    }

    public async Task<(bool Success, string Message)> ChangeSubscriptionAsync(int userId, string newPlan)
    {
        var validPlans = new[] { "Rookie", "Intermediate", "Advanced" };
        if (!validPlans.Contains(newPlan))
            return (false, "Ongeldig abonnementstype.");

        var newCredits = newPlan switch
        {
            "Rookie"       => 9,
            "Intermediate" => 13,
            "Advanced"     => 999,
            _              => 0
        };

        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                UPDATE users
                SET subscription_type = @plan,
                    credits = @credits,
                    subscription_paused = 0
                WHERE id = @userId
                """;
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@plan", newPlan);
            command.Parameters.AddWithValue("@credits", newCredits);
            command.Parameters.AddWithValue("@userId", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true, $"Abonnement gewijzigd naar {newPlan}.")
                : (false, "Gebruiker niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing subscription: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het wijzigen van het abonnement.");
        }
    }
}