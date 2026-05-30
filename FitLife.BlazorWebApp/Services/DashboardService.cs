using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for fetching dashboard statistics from the database
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IConfiguration _configuration;

    public DashboardService(IConfiguration configuration)
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
    /// Retrieves all dashboard statistics including counts, today's lessons, popular workouts, and recent activity
    /// </summary>
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var stats = new DashboardStatsDto();

        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        // Get basic counts - members, instructors, lessons, reservations
        const string countsSql = """
            SELECT
                (SELECT COUNT(*) FROM users WHERE role = 'member') AS total_members,
                (SELECT COUNT(*) FROM users WHERE role = 'instructor') AS total_instructors,
                (SELECT COUNT(*) FROM lessons WHERE start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY) AND start_time <= DATE_ADD(NOW(), INTERVAL 7 DAY)) AS lessons_this_week,
                (SELECT COUNT(*) FROM reservations WHERE reservation_date >= DATE_SUB(NOW(), INTERVAL 7 DAY) AND is_cancelled = 0) AS reservations_this_week,
                (SELECT COUNT(*) FROM lessons WHERE DATE(start_time) = CURDATE()) AS lessons_today
            """;

        await using var countsCommand = new MySqlCommand(countsSql, connection);
        await using var countsReader = await countsCommand.ExecuteReaderAsync();

        if (await countsReader.ReadAsync())
        {
            stats.TotalMembers = countsReader.GetInt32("total_members");
            stats.TotalInstructors = countsReader.GetInt32("total_instructors");
            stats.TotalLessonsThisWeek = countsReader.GetInt32("lessons_this_week");
            stats.TotalReservationsThisWeek = countsReader.GetInt32("reservations_this_week");
            stats.UpcomingLessonsToday = countsReader.GetInt32("lessons_today");
        }
        await countsReader.CloseAsync();

        // Get today's lessons with all relevant details
        const string todaysLessonsSql = """
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
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u ON u.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE DATE(l.start_time) = CURDATE()
            ORDER BY l.start_time
            """;

        await using var todaysCommand = new MySqlCommand(todaysLessonsSql, connection);
        await using var todaysReader = await todaysCommand.ExecuteReaderAsync();

        while (await todaysReader.ReadAsync())
        {
            stats.TodaysLessons.Add(new LessonDto
            {
                Id = todaysReader.GetInt32("id"),
                StartTime = todaysReader.GetDateTime("start_time"),
                EndTime = todaysReader.GetDateTime("end_time"),
                MaxParticipants = todaysReader.GetInt32("max_participants"),
                WorkoutId = todaysReader.GetInt32("workout_id"),
                WorkoutName = todaysReader.GetString("workout_name"),
                InstructorId = todaysReader.IsDBNull(todaysReader.GetOrdinal("instructor_id")) ? 0 : todaysReader.GetInt32("instructor_id"),
                InstructorName = todaysReader.IsDBNull(todaysReader.GetOrdinal("instructor_name")) ? "Niet toegewezen" : todaysReader.GetString("instructor_name"),
                LocationId = todaysReader.GetInt32("location_id"),
                LocationName = todaysReader.GetString("location_name"),
                CurrentParticipants = todaysReader.GetInt32("current_participants"),
                WaitlistCount = todaysReader.GetInt32("waitlist_count")
            });
        }
        await todaysReader.CloseAsync();

        // Get popular workouts based on reservation counts in the last 30 days
        const string popularWorkoutsSql = """
            SELECT
                w.name AS workout_name,
                COUNT(DISTINCT r.id) AS total_reservations,
                ROUND(AVG(CASE
                    WHEN COALESCE(l.capacity_override, w.default_capacity, 0) > 0
                    THEN ((SELECT COUNT(*) FROM reservations r2 WHERE r2.lesson_id = l.id AND r2.is_cancelled = 0) * 100.0 / COALESCE(l.capacity_override, w.default_capacity, 1))
                    ELSE 0
                END), 1) AS avg_occupancy
            FROM workouts w
            INNER JOIN lessons l ON l.workout_id = w.id
            LEFT JOIN reservations r ON r.lesson_id = l.id AND r.is_cancelled = 0
            WHERE l.start_time >= DATE_SUB(NOW(), INTERVAL 30 DAY)
            GROUP BY w.id, w.name
            HAVING total_reservations > 0
            ORDER BY total_reservations DESC
            LIMIT 5
            """;

        await using var popularCommand = new MySqlCommand(popularWorkoutsSql, connection);
        await using var popularReader = await popularCommand.ExecuteReaderAsync();

        while (await popularReader.ReadAsync())
        {
            stats.PopularWorkouts.Add(new PopularWorkoutDto
            {
                WorkoutName = popularReader.GetString("workout_name"),
                TotalReservations = popularReader.GetInt32("total_reservations"),
                AverageOccupancy = popularReader.IsDBNull(popularReader.GetOrdinal("avg_occupancy"))
                    ? 0
                    : popularReader.GetDouble("avg_occupancy")
            });
        }
        await popularReader.CloseAsync();

        // Get recent activities - last 10 reservations
        const string recentActivitiesSql = """
            SELECT
                CONCAT('Reservering voor ', w.name, ' door ', COALESCE(u.display_name, u.email)) AS description,
                r.reservation_date AS timestamp,
                'reservation' AS activity_type
            FROM reservations r
            INNER JOIN users u ON u.id = r.member_id
            INNER JOIN lessons l ON l.id = r.lesson_id
            INNER JOIN workouts w ON w.id = l.workout_id
            WHERE r.is_cancelled = 0
            ORDER BY r.reservation_date DESC
            LIMIT 10
            """;

        await using var activityCommand = new MySqlCommand(recentActivitiesSql, connection);
        await using var activityReader = await activityCommand.ExecuteReaderAsync();

        while (await activityReader.ReadAsync())
        {
            stats.RecentActivities.Add(new RecentActivityDto
            {
                Description = activityReader.GetString("description"),
                Timestamp = activityReader.GetDateTime("timestamp"),
                ActivityType = activityReader.GetString("activity_type")
            });
        }
        await activityReader.CloseAsync();

        // Calculate average occupancy percentage for the past week
        const string avgOccupancySql = """
            SELECT COALESCE(AVG(occupancy_pct), 0) AS avg_occupancy
            FROM (
                SELECT
                    CASE
                        WHEN COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) > 0
                        THEN (
                            (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) * 100.0
                            / COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 1)
                        )
                        ELSE 0
                    END AS occupancy_pct
                FROM lessons l
                INNER JOIN workouts w ON w.id = l.workout_id
                INNER JOIN locations loc ON loc.id = l.location_id
                WHERE l.start_time >= DATE_SUB(NOW(), INTERVAL 7 DAY) AND l.start_time <= NOW()
            ) AS occupancy_data
            """;

        await using var avgCommand = new MySqlCommand(avgOccupancySql, connection);
        var avgResult = await avgCommand.ExecuteScalarAsync();
        stats.AverageOccupancyPercentage = avgResult != null && avgResult != DBNull.Value
            ? Math.Round(Convert.ToDouble(avgResult), 1)
            : 0;

        return stats;
    }

    public async Task<InstructorDashboardDto> GetInstructorDashboardAsync(int userId)
    {
        var result = new InstructorDashboardDto();

        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        // Counts: total given, upcoming, unique students
        const string countSql = """
            SELECT
                (SELECT COUNT(*) FROM lessons WHERE instructor_id = @userId AND start_time < NOW()) AS total_given,
                (SELECT COUNT(*) FROM lessons WHERE instructor_id = @userId AND start_time >= NOW()) AS total_upcoming,
                (SELECT COUNT(DISTINCT r.member_id) FROM reservations r
                 INNER JOIN lessons l ON l.id = r.lesson_id
                 WHERE l.instructor_id = @userId AND r.is_cancelled = 0) AS total_students
            """;

        await using var countCmd = new MySqlCommand(countSql, connection);
        countCmd.Parameters.AddWithValue("@userId", userId);
        await using var countReader = await countCmd.ExecuteReaderAsync();
        if (await countReader.ReadAsync())
        {
            result.TotalLessonsGiven = countReader.GetInt32("total_given");
            result.TotalUpcomingLessons = countReader.GetInt32("total_upcoming");
            result.TotalStudentsTaught = countReader.GetInt32("total_students");
        }
        await countReader.CloseAsync();

        // Todays lessons
        const string todaySql = """
            SELECT l.id, l.start_time,
                DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
                COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
                l.workout_id, w.name AS workout_name,
                l.instructor_id, u.display_name AS instructor_name,
                l.location_id, loc.name AS location_name,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u ON u.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.instructor_id = @userId AND DATE(l.start_time) = CURDATE()
            ORDER BY l.start_time
            """;

        await using var todayCmd = new MySqlCommand(todaySql, connection);
        todayCmd.Parameters.AddWithValue("@userId", userId);
        await using var todayReader = await todayCmd.ExecuteReaderAsync();
        while (await todayReader.ReadAsync())
            result.TodaysLessons.Add(MapLessonRow(todayReader));
        await todayReader.CloseAsync();

        // Recent past lessons (last 10)
        const string recentSql = """
            SELECT l.id, l.start_time,
                DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
                COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
                l.workout_id, w.name AS workout_name,
                l.instructor_id, u.display_name AS instructor_name,
                l.location_id, loc.name AS location_name,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u ON u.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.instructor_id = @userId AND l.start_time < NOW()
            ORDER BY l.start_time DESC
            LIMIT 10
            """;

        await using var recentCmd = new MySqlCommand(recentSql, connection);
        recentCmd.Parameters.AddWithValue("@userId", userId);
        await using var recentReader = await recentCmd.ExecuteReaderAsync();
        while (await recentReader.ReadAsync())
            result.RecentLessons.Add(MapLessonRow(recentReader));
        await recentReader.CloseAsync();

        // Upcoming lessons (next 10)
        const string upcomingSql = """
            SELECT l.id, l.start_time,
                DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
                COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
                l.workout_id, w.name AS workout_name,
                l.instructor_id, u.display_name AS instructor_name,
                l.location_id, loc.name AS location_name,
                (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
                (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u ON u.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.instructor_id = @userId AND l.start_time > NOW()
            ORDER BY l.start_time ASC
            LIMIT 10
            """;

        await using var upcomingCmd = new MySqlCommand(upcomingSql, connection);
        upcomingCmd.Parameters.AddWithValue("@userId", userId);
        await using var upcomingReader = await upcomingCmd.ExecuteReaderAsync();
        while (await upcomingReader.ReadAsync())
            result.UpcomingLessons.Add(MapLessonRow(upcomingReader));
        await upcomingReader.CloseAsync();

        // Top workouts given by instructor
        const string workoutStatSql = """
            SELECT w.name AS workout_name,
                COUNT(DISTINCT l.id) AS lesson_count,
                COUNT(DISTINCT r.member_id) AS total_students
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN reservations r ON r.lesson_id = l.id AND r.is_cancelled = 0
            WHERE l.instructor_id = @userId
            GROUP BY w.id, w.name
            ORDER BY lesson_count DESC
            LIMIT 5
            """;

        await using var wsCmd = new MySqlCommand(workoutStatSql, connection);
        wsCmd.Parameters.AddWithValue("@userId", userId);
        await using var wsReader = await wsCmd.ExecuteReaderAsync();
        while (await wsReader.ReadAsync())
        {
            result.TopWorkouts.Add(new InstructorWorkoutStatDto
            {
                WorkoutName = wsReader.GetString("workout_name"),
                LessonCount = wsReader.GetInt32("lesson_count"),
                TotalStudents = wsReader.GetInt32("total_students")
            });
        }
        await wsReader.CloseAsync();

        // Lessons where instructor participated as a member
        const string participatedSql = """
            SELECT l.id, l.start_time,
                DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
                COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
                l.workout_id, w.name AS workout_name,
                l.instructor_id, u2.display_name AS instructor_name,
                l.location_id, loc.name AS location_name,
                (SELECT COUNT(*) FROM reservations r2 WHERE r2.lesson_id = l.id AND r2.is_cancelled = 0) AS current_participants,
                0 AS waitlist_count
            FROM reservations r
            INNER JOIN lessons l ON l.id = r.lesson_id
            INNER JOIN workouts w ON w.id = l.workout_id
            LEFT JOIN users u2 ON u2.id = l.instructor_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE r.member_id = @userId AND r.is_cancelled = 0
            ORDER BY l.start_time DESC
            LIMIT 10
            """;

        await using var partCmd = new MySqlCommand(participatedSql, connection);
        partCmd.Parameters.AddWithValue("@userId", userId);
        await using var partReader = await partCmd.ExecuteReaderAsync();
        while (await partReader.ReadAsync())
            result.ParticipatedLessons.Add(MapLessonRow(partReader));
        await partReader.CloseAsync();

        return result;
    }

    private static LessonDto MapLessonRow(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        StartTime = r.GetDateTime("start_time"),
        EndTime = r.GetDateTime("end_time"),
        MaxParticipants = r.GetInt32("max_participants"),
        WorkoutId = r.GetInt32("workout_id"),
        WorkoutName = r.GetString("workout_name"),
        InstructorId = r.IsDBNull(r.GetOrdinal("instructor_id")) ? 0 : r.GetInt32("instructor_id"),
        InstructorName = r.IsDBNull(r.GetOrdinal("instructor_name")) ? "Niet toegewezen" : r.GetString("instructor_name"),
        LocationId = r.GetInt32("location_id"),
        LocationName = r.GetString("location_name"),
        CurrentParticipants = r.GetInt32("current_participants"),
        WaitlistCount = r.GetInt32("waitlist_count")
    };
}
