using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

// Service for all lesson-related database operations in the Blazor admin panel.
// Talks directly to MySQL — no REST API layer.
public class LessonService : ILessonService
{
    private readonly IConfiguration _configuration;

    public LessonService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString() =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection not configured.");

    // Returns all lessons, optionally filtered to only archived (past) lessons.
    // Uses the is_archived flag set nightly by LessonArchiveService.
    // Joins workouts, users (instructor), and locations to get display names in one query.
    public async Task<List<LessonDto>> GetAllLessonsAsync(DateTime? fromDate = null, DateTime? toDate = null, bool archivedOnly = false)
    {
        var lessons = new List<LessonDto>();
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        // Build the base query — date filters are appended dynamically below
        var sql = """
            SELECT
                l.id,
                l.start_time,
                DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
                COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
                l.workout_id,
                w.name AS workout_name,
                l.instructor_id,
                u.display_name AS instructor_name,
                l.location_id,
                loc.name AS location_name,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w   ON w.id   = l.workout_id
            LEFT  JOIN users u      ON u.id   = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE 1=1
            """;

        // Toggle between active and archived lessons using the is_archived flag
        sql += archivedOnly ? " AND l.is_archived = 1" : " AND l.is_archived = 0";
        if (fromDate.HasValue) sql += " AND l.start_time >= @fromDate";
        if (toDate.HasValue)   sql += " AND l.start_time <= @toDate";
        sql += archivedOnly ? " ORDER BY l.start_time DESC" : " ORDER BY l.start_time ASC";

        await using var command = new MySqlCommand(sql, connection);
        if (fromDate.HasValue) command.Parameters.AddWithValue("@fromDate", fromDate.Value);
        if (toDate.HasValue)   command.Parameters.AddWithValue("@toDate",   toDate.Value);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            lessons.Add(MapLessonFromReader(reader));

        return lessons;
    }

    // Returns all lessons taught by a specific instructor, optionally from a given date onward.
    public async Task<List<LessonDto>> GetLessonsByInstructorAsync(int instructorId, DateTime? fromDate = null)
    {
        var lessons = new List<LessonDto>();
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        var sql = """
            SELECT
                l.id,
                l.start_time,
                DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
                COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
                l.workout_id,
                w.name AS workout_name,
                l.instructor_id,
                u.display_name AS instructor_name,
                l.location_id,
                loc.name AS location_name,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w    ON w.id   = l.workout_id
            LEFT  JOIN users u       ON u.id   = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.instructor_id = @instructorId
            """;

        if (fromDate.HasValue) sql += " AND l.start_time >= @fromDate";
        sql += " ORDER BY l.start_time ASC";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@instructorId", instructorId);
        if (fromDate.HasValue) command.Parameters.AddWithValue("@fromDate", fromDate.Value);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            lessons.Add(MapLessonFromReader(reader));

        return lessons;
    }

    // Fetches a single lesson by primary key. Returns null when not found.
    public async Task<LessonDto?> GetLessonByIdAsync(int lessonId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                l.id,
                l.start_time,
                DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
                COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
                l.workout_id,
                w.name AS workout_name,
                l.instructor_id,
                u.display_name AS instructor_name,
                l.location_id,
                loc.name AS location_name,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w    ON w.id   = l.workout_id
            LEFT  JOIN users u       ON u.id   = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.id = @lessonId
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@lessonId", lessonId);
        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ? MapLessonFromReader(reader) : null;
    }

    // Inserts a new lesson row. capacity_override is optional (NULL means use location/workout default).
    public async Task<(bool Success, string Message)> CreateLessonAsync(LessonEditDto lesson)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO lessons (start_time, duration_minutes, capacity_override, workout_id, instructor_id, location_id)
                VALUES (@startTime, @durationMinutes, @capacityOverride, @workoutId, @instructorId, @locationId);
                SELECT LAST_INSERT_ID();
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@startTime",        lesson.StartTime);
            command.Parameters.AddWithValue("@durationMinutes",  lesson.DurationMinutes);
            // Use DBNull.Value when no override is specified so the DB stores NULL
            command.Parameters.AddWithValue("@capacityOverride", lesson.CapacityOverride.HasValue
                                                                  ? lesson.CapacityOverride
                                                                  : DBNull.Value);
            command.Parameters.AddWithValue("@workoutId",    lesson.WorkoutId);
            command.Parameters.AddWithValue("@instructorId", lesson.InstructorId);
            command.Parameters.AddWithValue("@locationId",   lesson.LocationId);

            var result = await command.ExecuteScalarAsync();
            return (true, $"Les succesvol aangemaakt met ID {result}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating lesson: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van de les.");
        }
    }

    // Updates all editable fields of an existing lesson.
    public async Task<(bool Success, string Message)> UpdateLessonAsync(LessonEditDto lesson)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                UPDATE lessons SET
                    start_time        = @startTime,
                    duration_minutes  = @durationMinutes,
                    capacity_override = @capacityOverride,
                    workout_id        = @workoutId,
                    instructor_id     = @instructorId,
                    location_id       = @locationId
                WHERE id = @id
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id",               lesson.Id);
            command.Parameters.AddWithValue("@startTime",        lesson.StartTime);
            command.Parameters.AddWithValue("@durationMinutes",  lesson.DurationMinutes);
            command.Parameters.AddWithValue("@capacityOverride", lesson.CapacityOverride.HasValue
                                                                  ? lesson.CapacityOverride
                                                                  : DBNull.Value);
            command.Parameters.AddWithValue("@workoutId",    lesson.WorkoutId);
            command.Parameters.AddWithValue("@instructorId", lesson.InstructorId);
            command.Parameters.AddWithValue("@locationId",   lesson.LocationId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true,  "Les succesvol bijgewerkt.")
                : (false, "Les niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating lesson: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van de les.");
        }
    }

    // Deletes a lesson only if it has no active (non-cancelled) reservations.
    public async Task<(bool Success, string Message)> DeleteLessonAsync(int lessonId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            // Block deletion if members are still enrolled
            const string checkSql = "SELECT COUNT(*) FROM reservations WHERE lesson_id = @lessonId AND is_cancelled = 0";
            await using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@lessonId", lessonId);

            var reservationCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (reservationCount > 0)
                return (false, $"Kan les niet verwijderen: er zijn nog {reservationCount} actieve reserveringen.");

            const string sql = "DELETE FROM lessons WHERE id = @lessonId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@lessonId", lessonId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true,  "Les succesvol verwijderd.")
                : (false, "Les niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting lesson: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van de les.");
        }
    }

    // Returns all reservations (active and cancelled) for a given lesson,
    // joined with member details for the admin view.
    public async Task<List<ReservationDto>> GetLessonParticipantsAsync(int lessonId)
    {
        var participants = new List<ReservationDto>();
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                r.id,
                r.reservation_date,
                r.is_cancelled,
                r.member_id,
                u.display_name AS member_name,
                u.email        AS member_email
            FROM reservations r
            INNER JOIN users u ON u.id = r.member_id
            WHERE r.lesson_id = @lessonId
            ORDER BY r.reservation_date
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@lessonId", lessonId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            participants.Add(new ReservationDto
            {
                Id              = reader.GetInt32("id"),
                ReservationDate = reader.GetDateTime("reservation_date"),
                IsCancelled     = reader.GetBoolean("is_cancelled"),
                MemberId        = reader.GetInt32("member_id"),
                MemberName      = reader.IsDBNull(reader.GetOrdinal("member_name"))  ? "Onbekend" : reader.GetString("member_name"),
                MemberEmail     = reader.IsDBNull(reader.GetOrdinal("member_email")) ? null       : reader.GetString("member_email"),
                LessonId        = lessonId
            });
        }

        return participants;
    }

    // Maps a database row from the DataReader to a LessonDto object.
    // Handles nullable columns (instructor may be null if not yet assigned).
    private static LessonDto MapLessonFromReader(MySqlDataReader reader) => new()
    {
        Id                  = reader.GetInt32("id"),
        StartTime           = reader.GetDateTime("start_time"),
        EndTime             = reader.GetDateTime("end_time"),
        MaxParticipants     = reader.GetInt32("max_participants"),
        WorkoutId           = reader.GetInt32("workout_id"),
        WorkoutName         = reader.GetString("workout_name"),
        InstructorId        = reader.IsDBNull(reader.GetOrdinal("instructor_id")) ? 0 : reader.GetInt32("instructor_id"),
        InstructorName      = reader.IsDBNull(reader.GetOrdinal("instructor_name")) ? "Niet toegewezen" : reader.GetString("instructor_name"),
        LocationId          = reader.GetInt32("location_id"),
        LocationName        = reader.GetString("location_name"),
        CurrentParticipants = reader.GetInt32("current_participants"),
        WaitlistCount       = reader.GetInt32("waitlist_count")
    };
}
