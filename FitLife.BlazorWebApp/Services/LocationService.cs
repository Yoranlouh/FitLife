using FitLife.BlazorWebApp.Models;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Service for managing locations in the database
/// </summary>
public class LocationService : ILocationService
{
    private readonly IConfiguration _configuration;

    public LocationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
               ?? throw new InvalidOperationException("Database connection not configured.");
    }

    /// <summary>
    /// Retrieves all locations
    /// </summary>
    public async Task<List<LocationDto>> GetAllLocationsAsync()
    {
        var locations = new List<LocationDto>();
        
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                l.id,
                l.name,
                l.capacity,
                (SELECT COUNT(*) FROM lessons les WHERE les.location_id = l.id) AS total_lessons
            FROM locations l
            ORDER BY l.name
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            locations.Add(new LocationDto
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Capacity = reader.GetInt32("capacity"),
                TotalLessons = reader.GetInt32("total_lessons")
            });
        }

        return locations;
    }

    /// <summary>
    /// Gets a single location by ID
    /// </summary>
    public async Task<LocationDto?> GetLocationByIdAsync(int locationId)
    {
        await using var connection = new MySqlConnection(GetConnectionString());
        await connection.OpenAsync();

        const string sql = """
            SELECT
                l.id,
                l.name,
                l.capacity,
                (SELECT COUNT(*) FROM lessons les WHERE les.location_id = l.id) AS total_lessons
            FROM locations l
            WHERE l.id = @locationId
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@locationId", locationId);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new LocationDto
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Capacity = reader.GetInt32("capacity"),
                TotalLessons = reader.GetInt32("total_lessons")
            };
        }

        return null;
    }

    /// <summary>
    /// Creates a new location
    /// </summary>
    public async Task<(bool Success, string Message)> CreateLocationAsync(LocationDto location)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO locations (name, address, capacity)
                VALUES (@name, @address, @capacity);
                SELECT LAST_INSERT_ID();
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@name", location.Name);
            command.Parameters.AddWithValue("@address", location.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@capacity", location.Capacity);

            var result = await command.ExecuteScalarAsync();
            
            return (true, $"Locatie succesvol aangemaakt met ID {result}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating location: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het aanmaken van de locatie.");
        }
    }

    /// <summary>
    /// Updates an existing location
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateLocationAsync(LocationDto location)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                UPDATE locations SET
                    name = @name,
                    address = @address,
                    capacity = @capacity
                WHERE id = @id
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", location.Id);
            command.Parameters.AddWithValue("@name", location.Name);
            command.Parameters.AddWithValue("@address", location.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@capacity", location.Capacity);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Locatie succesvol bijgewerkt.") 
                : (false, "Locatie niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating location: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het bijwerken van de locatie.");
        }
    }

    /// <summary>
    /// Deletes a location
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteLocationAsync(int locationId)
    {
        try
        {
            await using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string checkSql = "SELECT COUNT(*) FROM lessons WHERE location_id = @locationId";
            await using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@locationId", locationId);
            
            var lessonCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (lessonCount > 0)
            {
                return (false, $"Kan locatie niet verwijderen: er zijn nog {lessonCount} gekoppelde lessen.");
            }

            const string sql = "DELETE FROM locations WHERE id = @locationId";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@locationId", locationId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            return rowsAffected > 0 
                ? (true, "Locatie succesvol verwijderd.") 
                : (false, "Locatie niet gevonden.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting location: {ex.Message}");
            return (false, "Er is een fout opgetreden bij het verwijderen van de locatie.");
        }
    }
}