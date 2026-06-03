using FitLife.BlazorWebApp.Models;
using MySqlConnector;

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
