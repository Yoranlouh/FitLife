using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for managing lessons in the database
/// </summary>
public class LessonService : ILessonService
{
    private readonly IConfiguration _configuration;

    public LessonService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the database connection string from configuration
    /// </summary>
    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
               ?? throw new InvalidOperationException("Database connection not configured.");
    }

    /// <summary>
    /// Retrieves all lessons within the specified date range
    /// </summary>
    public async Task<List<LessonDto>> GetAllLessonsAsync(DateTime? fromDate = null, DateTime? toDate = null)
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
                l.is_recurring,
                l.recurrence_pattern,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u ON u.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE 1=1
            """;

        if (fromDate.HasValue)
        {
            sql += " AND l.start_time >= @fromDate";
        }
        if (toDate.HasValue)
        {
            sql += " AND l.start_time <= @toDate";
        }

        sql += " ORDER BY l.start_time DESC";

        await using var command = new MySqlCommand(sql, connection);
        
        if (fromDate.HasValue)
            command.Parameters.AddWithValue("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            command.Parameters.AddWithValue("@toDate", toDate.Value);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lessons.Add(MapLessonFromReader(reader));
        }

        return lessons;
    }

    /// <summary>
    /// Gets lessons assigned to a specific instructor
    /// </summary>
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
                l.is_recurring,
                l.recurrence_pattern,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u ON u.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.instructor_id = @instructorId
            """;

        if (fromDate.HasValue)
        {
            sql += " AND l.start_time >= @fromDate";
        }

        sql += " ORDER BY l.start_time ASC";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@instructorId", instructorId);
        
        if (fromDate.HasValue)
            command.Parameters.AddWithValue("@fromDate", fromDate.Value);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lessons.Add(MapLessonFromReader(reader));
        }

        return lessons;
    }

    /// <summary>
    /// Gets a single lesson by its ID
    /// </summary>
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
                l.is_recurring,
                l.recurrence_pattern,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u ON u.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.id = @lessonId
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@lessonId", lessonId);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapLessonFromReader(reader);
        }

        return null;
    }

    /// <summary>
    /// Creates a new lesson in the database
    /// </summary>
    public async Task<(bool Success, string Message)> CreateLessonAsync(LessonEditDto lesson)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO lessons (start_time, duration_minutes, capacity_override, workout_id, instructor_id, location_id, 
                                    is_recurring, recurrence_pattern, recurrence_interval, recurrence_end_date, recurrence_count)
                VALUES (@startTime, @durationMinutes, @capacityOverride, @workoutId, @instructorId, @locationId,
                        @isRecurring, @recurrencePattern, @recurrenceInterval, @recurrenceEndDate, @recurrenceCount);
                SELECT LAST_INSERT_ID();
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@startTime", lesson.StartTime);
            command.Parameters.AddWithValue("@durationMinutes", lesson.DurationMinutes);
            command.Parameters.AddWithValue("@capacityOverride", lesson.CapacityOverride.HasValue ? lesson.CapacityOverride : DBNull.Value);
            command.Parameters.AddWithValue("@workoutId", lesson.WorkoutId);
            command.Parameters.AddWithValue("@instructorId", lesson.InstructorId);
            command.Parameters.AddWithValue("@locationId", lesson.LocationId);
            command.Parameters.AddWithValue("@isRecurring", lesson.IsRecurring);
            command.Parameters.AddWithValue("@recurrencePattern", lesson.RecurrencePattern ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@recurrenceInterval", lesson.RecurrenceInterval ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@recurrenceEndDate", lesson.RecurrenceEndDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@recurrenceCount", lesson.RecurrenceCount ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            
            return (true, $"Les succesvol aangemaakt met ID {result}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating lesson: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van de les.");
        }
    }

    /// <summary>
    /// Updates an existing lesson in the database
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateLessonAsync(LessonEditDto lesson)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                UPDATE lessons SET
                    start_time = @startTime,
                    duration_minutes = @durationMinutes,
                    capacity_override = @capacityOverride,
                    workout_id = @workoutId,
                    instructor_id = @instructorId,
                    location_id = @locationId,
                    is_recurring = @isRecurring,
                    recurrence_pattern = @recurrencePattern,
                    recurrence_interval = @recurrenceInterval,
                    recurrence_end_date = @recurrenceEndDate,
                    recurrence_count = @recurrenceCount
                WHERE id = @id
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", lesson.Id);
            command.Parameters.AddWithValue("@startTime", lesson.StartTime);
            command.Parameters.AddWithValue("@durationMinutes", lesson.DurationMinutes);
            command.Parameters.AddWithValue("@capacityOverride", lesson.CapacityOverride.HasValue ? lesson.CapacityOverride : DBNull.Value);
            command.Parameters.AddWithValue("@workoutId", lesson.WorkoutId);
            command.Parameters.AddWithValue("@instructorId", lesson.InstructorId);
            command.Parameters.AddWithValue("@locationId", lesson.LocationId);
            command.Parameters.AddWithValue("@isRecurring", lesson.IsRecurring);
            command.Parameters.AddWithValue("@recurrencePattern", lesson.RecurrencePattern ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@recurrenceInterval", lesson.RecurrenceInterval ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@recurrenceEndDate", lesson.RecurrenceEndDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@recurrenceCount", lesson.RecurrenceCount ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Les succesvol bijgewerkt.") 
                : (false, "Les niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating lesson: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van de les.");
        }
    }

    /// <summary>
    /// Deletes a lesson from the database
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteLessonAsync(int lessonId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            // First check if there are any reservations
            const string checkSql = "SELECT COUNT(*) FROM reservations WHERE lesson_id = @lessonId AND is_cancelled = 0";
            await using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@lessonId", lessonId);
            
            var reservationCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (reservationCount > 0)
            {
                return (false, $"Kan les niet verwijderen: er zijn nog {reservationCount} actieve reserveringen.");
            }

            const string sql = "DELETE FROM lessons WHERE id = @lessonId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@lessonId", lessonId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Les succesvol verwijderd.") 
                : (false, "Les niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting lesson: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van de les.");
        }
    }

    /// <summary>
    /// Gets all participants (reservations) for a specific lesson
    /// </summary>
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
                u.email AS member_email
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
                Id = reader.GetInt32("id"),
                ReservationDate = reader.GetDateTime("reservation_date"),
                IsCancelled = reader.GetBoolean("is_cancelled"),
                MemberId = reader.GetInt32("member_id"),
                MemberName = reader.IsDBNull(reader.GetOrdinal("member_name")) 
                    ? "Onbekend" 
                    : reader.GetString("member_name"),
                MemberEmail = reader.IsDBNull(reader.GetOrdinal("member_email")) 
                    ? null 
                    : reader.GetString("member_email"),
                LessonId = lessonId
            });
        }

        return participants;
    }

    /// <summary>
    /// Maps a database reader row to a LessonDto object
    /// </summary>
    private static LessonDto MapLessonFromReader(MySqlDataReader reader)
    {
        return new LessonDto
        {
            Id = reader.GetInt32("id"),
            StartTime = reader.GetDateTime("start_time"),
            EndTime = reader.GetDateTime("end_time"),
            MaxParticipants = reader.GetInt32("max_participants"),
            WorkoutId = reader.GetInt32("workout_id"),
            WorkoutName = reader.GetString("workout_name"),
            InstructorId = reader.IsDBNull(reader.GetOrdinal("instructor_id")) ? 0 : reader.GetInt32("instructor_id"),
            InstructorName = reader.IsDBNull(reader.GetOrdinal("instructor_name")) ? "Niet toegewezen" : reader.GetString("instructor_name"),
            LocationId = reader.GetInt32("location_id"),
            LocationName = reader.GetString("location_name"),
            IsRecurring = !reader.IsDBNull(reader.GetOrdinal("is_recurring")) && reader.GetBoolean("is_recurring"),
            RecurrencePattern = reader.IsDBNull(reader.GetOrdinal("recurrence_pattern")) ? null : reader.GetString("recurrence_pattern"),
            CurrentParticipants = reader.GetInt32("current_participants"),
            WaitlistCount = reader.GetInt32("waitlist_count")
        };
    }
}