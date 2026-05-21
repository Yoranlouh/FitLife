using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for managing instructors in the database
/// </summary>
public class InstructorService : IInstructorService
{
    private readonly IConfiguration _configuration;

    public InstructorService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
               ?? throw new InvalidOperationException("Database connection not configured.");
    }

    /// <summary>
    /// Retrieves all instructors with their lesson counts
    /// </summary>
    public async Task<List<InstructorDto>> GetAllInstructorsAsync()
    {
        var instructors = new List<InstructorDto>();
        
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                i.id,
                i.first_name,
                i.last_name,
                i.email,
                i.specialization,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = i.id) AS total_lessons,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = i.id AND l.start_time >= NOW()) AS upcoming_lessons
            FROM instructors i
            ORDER BY i.last_name, i.first_name
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            instructors.Add(new InstructorDto
            {
                Id = reader.GetInt32("id"),
                FirstName = reader.GetString("first_name"),
                LastName = reader.GetString("last_name"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Specialization = reader.IsDBNull(reader.GetOrdinal("specialization")) ? null : reader.GetString("specialization"),
                TotalLessons = reader.GetInt32("total_lessons"),
                UpcomingLessons = reader.GetInt32("upcoming_lessons")
            });
        }

        return instructors;
    }

    /// <summary>
    /// Gets a single instructor by their ID
    /// </summary>
    public async Task<InstructorDto?> GetInstructorByIdAsync(int instructorId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                i.id,
                i.first_name,
                i.last_name,
                i.email,
                i.specialization,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = i.id) AS total_lessons,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = i.id AND l.start_time >= NOW()) AS upcoming_lessons
            FROM instructors i
            WHERE i.id = @instructorId
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@instructorId", instructorId);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new InstructorDto
            {
                Id = reader.GetInt32("id"),
                FirstName = reader.GetString("first_name"),
                LastName = reader.GetString("last_name"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Specialization = reader.IsDBNull(reader.GetOrdinal("specialization")) ? null : reader.GetString("specialization"),
                TotalLessons = reader.GetInt32("total_lessons"),
                UpcomingLessons = reader.GetInt32("upcoming_lessons")
            };
        }

        return null;
    }

    /// <summary>
    /// Gets an instructor by their linked user ID
    /// </summary>
    public async Task<InstructorDto?> GetInstructorByUserIdAsync(int userId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        // Get instructor ID linked to user via email match
        const string sql = """
            SELECT
                i.id,
                i.first_name,
                i.last_name,
                i.email,
                i.specialization,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = i.id) AS total_lessons,
                (SELECT COUNT(*) FROM lessons l WHERE l.instructor_id = i.id AND l.start_time >= NOW()) AS upcoming_lessons
            FROM instructors i
            INNER JOIN users u ON u.email = i.email
            WHERE u.id = @userId
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new InstructorDto
            {
                Id = reader.GetInt32("id"),
                FirstName = reader.GetString("first_name"),
                LastName = reader.GetString("last_name"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Specialization = reader.IsDBNull(reader.GetOrdinal("specialization")) ? null : reader.GetString("specialization"),
                TotalLessons = reader.GetInt32("total_lessons"),
                UpcomingLessons = reader.GetInt32("upcoming_lessons")
            };
        }

        return null;
    }

    /// <summary>
    /// Creates a new instructor record
    /// </summary>
    public async Task<(bool Success, string Message)> CreateInstructorAsync(InstructorDto instructor)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO instructors (first_name, last_name, email, specialization)
                VALUES (@firstName, @lastName, @email, @specialization);
                SELECT LAST_INSERT_ID();
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@firstName", instructor.FirstName);
            command.Parameters.AddWithValue("@lastName", instructor.LastName);
            command.Parameters.AddWithValue("@email", instructor.Email ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@specialization", instructor.Specialization ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            
            return (true, $"Instructeur succesvol aangemaakt met ID {result}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating instructor: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van de instructeur.");
        }
    }

    /// <summary>
    /// Updates an existing instructor record
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateInstructorAsync(InstructorDto instructor)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                UPDATE instructors SET
                    first_name = @firstName,
                    last_name = @lastName,
                    email = @email,
                    specialization = @specialization
                WHERE id = @id
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", instructor.Id);
            command.Parameters.AddWithValue("@firstName", instructor.FirstName);
            command.Parameters.AddWithValue("@lastName", instructor.LastName);
            command.Parameters.AddWithValue("@email", instructor.Email ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@specialization", instructor.Specialization ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Instructeur succesvol bijgewerkt.") 
                : (false, "Instructeur niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating instructor: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van de instructeur.");
        }
    }

    /// <summary>
    /// Deletes an instructor record
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteInstructorAsync(int instructorId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            // Check if instructor has any lessons
            const string checkSql = "SELECT COUNT(*) FROM lessons WHERE instructor_id = @instructorId";
            await using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@instructorId", instructorId);
            
            var lessonCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (lessonCount > 0)
            {
                return (false, $"Kan instructeur niet verwijderen: er zijn nog {lessonCount} gekoppelde lessen.");
            }

            const string sql = "DELETE FROM instructors WHERE id = @instructorId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@instructorId", instructorId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Instructeur succesvol verwijderd.") 
                : (false, "Instructeur niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting instructor: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van de instructeur.");
        }
    }
}