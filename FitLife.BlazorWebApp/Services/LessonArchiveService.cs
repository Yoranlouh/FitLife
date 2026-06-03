using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

// Background service that automatically archives past lessons in the database.
// Runs once at startup and then daily at midnight.
// A lesson is "archived" when its start date is in the past — it moves from the
// active lessons list to the archive view in the admin panel.
public class LessonArchiveService : BackgroundService
{
    private readonly IConfiguration                  _configuration;
    private readonly ILogger<LessonArchiveService>   _logger;

    public LessonArchiveService(IConfiguration configuration, ILogger<LessonArchiveService> logger)
    {
        _configuration = configuration;
        _logger        = logger;
    }

    // Entry point for the background service.
    // Archives past lessons immediately on startup, then waits until the next midnight
    // and repeats indefinitely until the application stops.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Archive any lessons that ended before today (e.g. from a previous app run)
        await ArchivePastLessonsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate how long to wait until the next midnight
            var delay = DateTime.Today.AddDays(1) - DateTime.Now;
            await Task.Delay(delay, stoppingToken);

            await ArchivePastLessonsAsync();
        }
    }

    // Sets is_archived = 1 for every lesson whose start_time is before today.
    // Uses a single UPDATE statement for efficiency — no row-by-row processing.
    // Errors are caught and logged so the background service never crashes the host.
    private async Task ArchivePastLessonsAsync()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString)) return;

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Only archive lessons that aren't already archived to avoid unnecessary writes
            const string sql = """
                UPDATE lessons
                SET is_archived = 1
                WHERE DATE(start_time) < CURDATE()
                  AND is_archived = 0
                """;

            await using var command = new MySqlCommand(sql, connection);
            var archived = await command.ExecuteNonQueryAsync();

            if (archived > 0)
                _logger.LogInformation("LessonArchiveService: {Count} lessen gearchiveerd.", archived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LessonArchiveService: fout bij archiveren van lessen.");
        }
    }
}
