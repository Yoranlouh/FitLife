using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

// Service for workout-type (fitness class types) management in the Blazor admin panel.
// Talks directly to MySQL — no REST API layer.
public class WorkoutService : IWorkoutService
{
    private readonly IConfiguration _configuration;

    // Static flag: ensures the one-time colour-column migration runs at most once
    // per process lifetime, even if multiple requests arrive before it completes.
    private static volatile bool _colorColumnReady = false;

    public WorkoutService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("Database connection not configured.");
    }

    // Adds the 'color' column to the workouts table if it does not yet exist.
    // Called before every query that reads the color column to handle legacy databases.
    // Uses a static flag to skip the check on subsequent requests (performance optimisation).
    private static async Task EnsureColorColumnAsync(MySqlConnection connection)
    {
        if (_colorColumnReady) return;
        try
        {
            const string checkSql = """
                SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME   = 'workouts'
                  AND COLUMN_NAME  = 'color'
                """;
            await using var checkCmd = new MySqlCommand(checkSql, connection);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                await using var addCmd = new MySqlCommand(
                    "ALTER TABLE workouts ADD COLUMN color VARCHAR(7) NULL DEFAULT NULL;",
                    connection);
                await addCmd.ExecuteNonQueryAsync();
            }

            _colorColumnReady = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WorkoutService] color kolom migratie: {ex.Message}");
        }
    }

    // Returns all workout types ordered alphabetically.
    // total_lessons counts how many lessons currently reference each workout type.
    public async Task<List<WorkoutDto>> GetAllWorkoutsAsync()
    {
        var workouts = new List<WorkoutDto>();

        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await EnsureColorColumnAsync(connection);

        const string sql = """
            SELECT
                w.id,
                w.name,
                w.description,
                w.default_capacity,
                w.color,
                (SELECT COUNT(*) FROM lessons l WHERE l.workout_id = w.id) AS total_lessons
            FROM workouts w
            ORDER BY w.name
            """;

        try
        {
            await using var command = new MySqlCommand(sql, connection);
            await using var reader  = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                workouts.Add(new WorkoutDto
                {
                    Id              = reader.GetInt32("id"),
                    Name            = reader.GetString("name"),
                    Description     = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                    DefaultCapacity = reader.GetInt32("default_capacity"),
                    // Fall back to the brand colour when no colour has been assigned yet
                    Color           = reader.IsDBNull(reader.GetOrdinal("color")) ? "#5B6636" : reader.GetString("color"),
                    TotalLessons    = reader.GetInt32("total_lessons")
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading workouts: {ex.Message}");
        }

        return workouts;
    }

    // Returns a single workout by primary key, or null when not found.
    public async Task<WorkoutDto?> GetWorkoutByIdAsync(int workoutId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await EnsureColorColumnAsync(connection);

        const string sql = """
            SELECT
                w.id,
                w.name,
                w.description,
                w.default_capacity,
                w.color,
                (SELECT COUNT(*) FROM lessons l WHERE l.workout_id = w.id) AS total_lessons
            FROM workouts w
            WHERE w.id = @workoutId
            """;

        try
        {
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@workoutId", workoutId);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new WorkoutDto
                {
                    Id              = reader.GetInt32("id"),
                    Name            = reader.GetString("name"),
                    Description     = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                    DefaultCapacity = reader.GetInt32("default_capacity"),
                    Color           = reader.IsDBNull(reader.GetOrdinal("color")) ? "#5B6636" : reader.GetString("color"),
                    TotalLessons    = reader.GetInt32("total_lessons")
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading workout {workoutId}: {ex.Message}");
        }

        return null;
    }

    // Inserts a new workout type. Returns the generated ID in the success message.
    public async Task<(bool Success, string Message)> CreateWorkoutAsync(WorkoutDto workout)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            await EnsureColorColumnAsync(connection);

            const string sql = """
                INSERT INTO workouts (name, description, default_capacity, color)
                VALUES (@name, @description, @defaultCapacity, @color);
                SELECT LAST_INSERT_ID();
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@name",            workout.Name);
            command.Parameters.AddWithValue("@description",     workout.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@defaultCapacity", workout.DefaultCapacity);
            // Use the brand colour as default when none is provided
            command.Parameters.AddWithValue("@color",           string.IsNullOrEmpty(workout.Color) ? "#5B6636" : workout.Color);

            var result = await command.ExecuteScalarAsync();
            return (true, $"Workout type succesvol aangemaakt met ID {result}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating workout: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van het workout type.");
        }
    }

    // Updates all editable fields of an existing workout type.
    public async Task<(bool Success, string Message)> UpdateWorkoutAsync(WorkoutDto workout)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            await EnsureColorColumnAsync(connection);

            const string sql = """
                UPDATE workouts SET
                    name             = @name,
                    description      = @description,
                    default_capacity = @defaultCapacity,
                    color            = @color
                WHERE id = @id
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id",              workout.Id);
            command.Parameters.AddWithValue("@name",            workout.Name);
            command.Parameters.AddWithValue("@description",     workout.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@defaultCapacity", workout.DefaultCapacity);
            command.Parameters.AddWithValue("@color",           string.IsNullOrEmpty(workout.Color) ? "#5B6636" : workout.Color);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0
                ? (true,  "Workout type succesvol bijgewerkt.")
                : (false, "Workout type niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating workout: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van het workout type.");
        }
    }

    // Deletes a workout type AND all lessons that use it (cascade delete).
    // This is intentionally aggressive — ensure the admin is warned before calling this.
    public async Task<(bool Success, string Message)> DeleteWorkoutAsync(int workoutId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            // Delete child lessons first to avoid a foreign key constraint violation
            const string deleteLessonsSql = "DELETE FROM lessons WHERE workout_id = @workoutId";
            await using var deleteLessonsCmd = new MySqlCommand(deleteLessonsSql, connection);
            deleteLessonsCmd.Parameters.AddWithValue("@workoutId", workoutId);
            await deleteLessonsCmd.ExecuteNonQueryAsync();

            // Now delete the workout type itself
            const string sql = "DELETE FROM workouts WHERE id = @workoutId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@workoutId", workoutId);
            var rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0
                ? (true,  "Workout succesvol verwijderd.")
                : (false, "Workout niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting workout: {ex.Message}");
            return (false, $"Fout bij verwijderen: {ex.Message}");
        }
    }
}
