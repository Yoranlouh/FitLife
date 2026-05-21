using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for managing workout types in the database
/// </summary>
public class WorkoutService : IWorkoutService
{
    private readonly IConfiguration _configuration;

    public WorkoutService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
               ?? throw new InvalidOperationException("Database connection not configured.");
    }

    /// <summary>
    /// Retrieves all workout types with their lesson counts
    /// </summary>
    public async Task<List<WorkoutDto>> GetAllWorkoutsAsync()
    {
        var workouts = new List<WorkoutDto>();
        
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                w.id,
                w.name,
                w.description,
                w.duration_minutes,
                w.default_capacity,
                (SELECT COUNT(*) FROM lessons l WHERE l.workout_id = w.id) AS total_lessons
            FROM workouts w
            ORDER BY w.name
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            workouts.Add(new WorkoutDto
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                DurationMinutes = reader.GetInt32("duration_minutes"),
                DefaultCapacity = reader.GetInt32("default_capacity"),
                TotalLessons = reader.GetInt32("total_lessons")
            });
        }

        return workouts;
    }

    /// <summary>
    /// Gets a single workout by ID
    /// </summary>
    public async Task<WorkoutDto?> GetWorkoutByIdAsync(int workoutId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                w.id,
                w.name,
                w.description,
                w.duration_minutes,
                w.default_capacity,
                (SELECT COUNT(*) FROM lessons l WHERE l.workout_id = w.id) AS total_lessons
            FROM workouts w
            WHERE w.id = @workoutId
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@workoutId", workoutId);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new WorkoutDto
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                DurationMinutes = reader.GetInt32("duration_minutes"),
                DefaultCapacity = reader.GetInt32("default_capacity"),
                TotalLessons = reader.GetInt32("total_lessons")
            };
        }

        return null;
    }

    /// <summary>
    /// Creates a new workout type
    /// </summary>
    public async Task<(bool Success, string Message)> CreateWorkoutAsync(WorkoutDto workout)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO workouts (name, description, duration_minutes, default_capacity)
                VALUES (@name, @description, @durationMinutes, @defaultCapacity);
                SELECT LAST_INSERT_ID();
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@name", workout.Name);
            command.Parameters.AddWithValue("@description", workout.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@durationMinutes", workout.DurationMinutes);
            command.Parameters.AddWithValue("@defaultCapacity", workout.DefaultCapacity);

            var result = await command.ExecuteScalarAsync();
            
            return (true, $"Workout type succesvol aangemaakt met ID {result}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating workout: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van het workout type.");
        }
    }

    /// <summary>
    /// Updates an existing workout type
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateWorkoutAsync(WorkoutDto workout)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                UPDATE workouts SET
                    name = @name,
                    description = @description,
                    duration_minutes = @durationMinutes,
                    default_capacity = @defaultCapacity
                WHERE id = @id
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", workout.Id);
            command.Parameters.AddWithValue("@name", workout.Name);
            command.Parameters.AddWithValue("@description", workout.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@durationMinutes", workout.DurationMinutes);
            command.Parameters.AddWithValue("@defaultCapacity", workout.DefaultCapacity);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Workout type succesvol bijgewerkt.") 
                : (false, "Workout type niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating workout: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van het workout type.");
        }
    }

    /// <summary>
    /// Deletes a workout type
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteWorkoutAsync(int workoutId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string checkSql = "SELECT COUNT(*) FROM lessons WHERE workout_id = @workoutId";
            await using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@workoutId", workoutId);
            
            var lessonCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (lessonCount > 0)
            {
                return (false, $"Kan workout type niet verwijderen: er zijn nog {lessonCount} gekoppelde lessen.");
            }

            const string sql = "DELETE FROM workouts WHERE id = @workoutId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@workoutId", workoutId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Workout type succesvol verwijderd.") 
                : (false, "Workout type niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting workout: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van het workout type.");
        }
    }
}