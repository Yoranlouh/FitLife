using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for managing reservations in the database
/// </summary>
public class ReservationService : IReservationService
{
    private readonly IConfiguration _configuration;

    public ReservationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
               ?? throw new InvalidOperationException("Database connection not configured.");
    }

    /// <summary>
    /// Retrieves all reservations within the specified date range
    /// </summary>
    public async Task<List<ReservationDto>> GetAllReservationsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var reservations = new List<ReservationDto>();
        
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        var sql = """
            SELECT
                r.id,
                r.reservation_date,
                r.is_cancelled,
                r.member_id,
                u.display_name AS member_name,
                u.email AS member_email,
                r.lesson_id,
                l.start_time AS lesson_start_time,
                CONCAT(w.name, ' - ', loc.name) AS lesson_info
            FROM reservations r
            INNER JOIN users u ON u.id = r.member_id
            INNER JOIN lessons l ON l.id = r.lesson_id
            INNER JOIN workouts w ON w.id = l.workout_id
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

        sql += " ORDER BY r.reservation_date DESC";

        await using var command = new MySqlCommand(sql, connection);
        
        if (fromDate.HasValue)
            command.Parameters.AddWithValue("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            command.Parameters.AddWithValue("@toDate", toDate.Value);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            reservations.Add(new ReservationDto
            {
                Id = reader.GetInt32("id"),
                ReservationDate = reader.GetDateTime("reservation_date"),
                IsCancelled = reader.GetBoolean("is_cancelled"),
                MemberId = reader.GetInt32("member_id"),
                MemberName = reader.IsDBNull(reader.GetOrdinal("member_name")) ? "Onbekend" : reader.GetString("member_name"),
                MemberEmail = reader.IsDBNull(reader.GetOrdinal("member_email")) ? null : reader.GetString("member_email"),
                LessonId = reader.GetInt32("lesson_id"),
                LessonStartTime = reader.GetDateTime("lesson_start_time"),
                LessonInfo = reader.GetString("lesson_info")
            });
        }

        return reservations;
    }

    /// <summary>
    /// Gets all reservations for a specific lesson
    /// </summary>
    public async Task<List<ReservationDto>> GetReservationsByLessonAsync(int lessonId)
    {
        var reservations = new List<ReservationDto>();
        
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                r.id,
                r.reservation_date,
                r.is_cancelled,
                r.member_id,
                u.display_name AS member_name,
                u.email AS member_email,
                r.lesson_id,
                l.start_time AS lesson_start_time,
                CONCAT(w.name, ' - ', loc.name) AS lesson_info
            FROM reservations r
            INNER JOIN users u ON u.id = r.member_id
            INNER JOIN lessons l ON l.id = r.lesson_id
            INNER JOIN workouts w ON w.id = l.workout_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE r.lesson_id = @lessonId
            ORDER BY r.reservation_date
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@lessonId", lessonId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            reservations.Add(new ReservationDto
            {
                Id = reader.GetInt32("id"),
                ReservationDate = reader.GetDateTime("reservation_date"),
                IsCancelled = reader.GetBoolean("is_cancelled"),
                MemberId = reader.GetInt32("member_id"),
                MemberName = reader.IsDBNull(reader.GetOrdinal("member_name")) ? "Onbekend" : reader.GetString("member_name"),
                MemberEmail = reader.IsDBNull(reader.GetOrdinal("member_email")) ? null : reader.GetString("member_email"),
                LessonId = reader.GetInt32("lesson_id"),
                LessonStartTime = reader.GetDateTime("lesson_start_time"),
                LessonInfo = reader.GetString("lesson_info")
            });
        }

        return reservations;
    }

    /// <summary>
    /// Cancels a reservation by setting is_cancelled to true
    /// </summary>
    public async Task<(bool Success, string Message)> CancelReservationAsync(int reservationId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = "UPDATE reservations SET is_cancelled = 1 WHERE id = @reservationId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@reservationId", reservationId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Reservering succesvol geannuleerd.") 
                : (false, "Reservering niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cancelling reservation: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het annuleren van de reservering.");
        }
    }
}