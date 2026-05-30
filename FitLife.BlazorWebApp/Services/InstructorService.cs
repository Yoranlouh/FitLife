using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

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
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            instructors.Add(MapFromReader(reader));

        return instructors;
    }

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
            WHERE u.id = @id AND u.role = 'instructor'
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", instructorId);
        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    // instructors ARE users — userId == instructorId
    public Task<InstructorDto?> GetInstructorByUserIdAsync(int userId) =>
        GetInstructorByIdAsync(userId);

    public async Task<(bool Success, string Message)> CreateInstructorAsync(InstructorDto instructor)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO users (display_name, email, role, password_hash)
                VALUES (@displayName, @email, 'instructor', '');
                SELECT LAST_INSERT_ID();
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@displayName", instructor.FirstName);
            command.Parameters.AddWithValue("@email", instructor.Email ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return (true, $"Instructeur aangemaakt met ID {result}.");
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
                UPDATE users SET display_name = @displayName, email = @email
                WHERE id = @id AND role = 'instructor'
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", instructor.Id);
            command.Parameters.AddWithValue("@displayName", instructor.FirstName);
            command.Parameters.AddWithValue("@email", instructor.Email ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true, "Instructeur bijgewerkt.")
                : (false, "Instructeur niet gevonden.");
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

            const string checkSql = "SELECT COUNT(*) FROM lessons WHERE instructor_id = @id";
            await using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@id", instructorId);

            var lessonCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (lessonCount > 0)
                return (false, $"Kan instructeur niet verwijderen: {lessonCount} gekoppelde lessen.");

            const string sql = "DELETE FROM users WHERE id = @id AND role = 'instructor'";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", instructorId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true, "Instructeur verwijderd.")
                : (false, "Instructeur niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting instructor: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van de instructeur.");
        }
    }

    private static InstructorDto MapFromReader(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("id"),
        FirstName = reader.IsDBNull(reader.GetOrdinal("display_name")) ? "Onbekend" : reader.GetString("display_name"),
        LastName = string.Empty,
        Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
        PhotoUrl = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? null : reader.GetString("photo_url"),
        TotalLessons = reader.GetInt32("total_lessons"),
        UpcomingLessons = reader.GetInt32("upcoming_lessons")
    };
}