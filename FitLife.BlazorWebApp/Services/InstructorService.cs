using FitLife.BlazorWebApp.Models;
using MySqlConnector;
using System.Linq;

namespace FitLife.BlazorWebApp.Services;

// Service for instructor (user with role = 'instructor') management operations
// in the Blazor admin panel. Reads and writes directly to MySQL.
public class InstructorService : IInstructorService
{
    private readonly IConfiguration _configuration;

    public InstructorService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString() =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection not configured.");

    // Returns all users with role = 'instructor', including lesson counts.
    // total_lessons   = all lessons ever assigned to this instructor
    // upcoming_lessons = lessons from now onwards (used for workload display)
    public async Task<List<InstructorDto>> GetAllInstructorsAsync()
    {
        var instructors = new List<InstructorDto>();
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                u.id,
                u.display_name,
                u.email,
                u.photo_url,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = u.id) AS total_lessons,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = u.id AND l.start_time >= NOW()) AS upcoming_lessons
            FROM users u
            WHERE u.role = 'instructor'
            ORDER BY u.display_name
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader  = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            instructors.Add(MapFromReader(reader));

        return instructors;
    }

    // Returns a single instructor by primary key, or null if not found.
    public async Task<InstructorDto?> GetInstructorByIdAsync(int instructorId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                u.id,
                u.display_name,
                u.email,
                u.photo_url,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = u.id) AS total_lessons,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = u.id AND l.start_time >= NOW()) AS upcoming_lessons
            FROM users u
            WHERE u.id = @instructorId AND u.role = 'instructor'
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@instructorId", instructorId);
        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public Task<InstructorDto?> GetInstructorByUserIdAsync(int userId) =>
        GetInstructorByIdAsync(userId);

    public async Task<(bool Success, string Message)> CreateInstructorAsync(InstructorDto instructor)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            var displayName = instructor.FullName.Trim();
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("FitLife@Instructor"));
            var passwordHash = string.Concat(hashBytes.Select(b => b.ToString("x2")));

            const string sql = """
                INSERT INTO users (email, password_hash, display_name, role, photo_url, credits, subscription_type)
                VALUES (@email, @passwordHash, @displayName, 'instructor', @photoUrl, 0, 'None')
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@email",        (object?)instructor.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@displayName",  displayName);
            command.Parameters.AddWithValue("@photoUrl",     (object?)instructor.PhotoUrl ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
            return (true, $"Instructeur {displayName} succesvol aangemaakt.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return (false, "Dit e-mailadres is al in gebruik.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating instructor: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van de instructeur.");
        }
    }

    public async Task<(bool Success, string Message)> UpdateInstructorAsync(InstructorDto instructor)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                UPDATE users
                SET display_name = @displayName,
                    email        = @email,
                    photo_url    = @photoUrl
                WHERE id = @id AND role = 'instructor'
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@displayName", instructor.FullName.Trim());
            command.Parameters.AddWithValue("@email",       (object?)instructor.Email   ?? DBNull.Value);
            command.Parameters.AddWithValue("@photoUrl",    (object?)instructor.PhotoUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@id",          instructor.Id);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0
                ? (true,  "Instructeur succesvol bijgewerkt.")
                : (false, "Instructeur niet gevonden.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return (false, "Dit e-mailadres is al in gebruik.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating instructor: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van de instructeur.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteInstructorAsync(int instructorId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = "DELETE FROM users WHERE id = @id AND role = 'instructor'";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", instructorId);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0
                ? (true,  "Instructeur succesvol verwijderd.")
                : (false, "Instructeur niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting instructor: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van de instructeur.");
        }
    }

    // Maps a DataReader row to an InstructorDto.
    // display_name is split into FirstName and LastName on the first space.
    private static InstructorDto MapFromReader(MySqlDataReader reader)
    {
        var displayName = reader.IsDBNull(reader.GetOrdinal("display_name"))
            ? ""
            : reader.GetString("display_name");

        // Split "Jan de Vries" into first="Jan", last="de Vries"
        var parts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        return new InstructorDto
        {
            Id             = reader.GetInt32("id"),
            FirstName      = parts.Length > 0 ? parts[0] : displayName,
            LastName       = parts.Length > 1 ? parts[1] : "",
            Email          = reader.IsDBNull(reader.GetOrdinal("email"))     ? null : reader.GetString("email"),
            PhotoUrl       = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? null : reader.GetString("photo_url"),
            TotalLessons   = reader.GetInt32("total_lessons"),
            UpcomingLessons = reader.GetInt32("upcoming_lessons")
        };
    }
}
