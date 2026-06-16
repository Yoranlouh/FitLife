using MySqlConnector;
using Scalar.AspNetCore;
using SharedLibrary.DTOs.Responses;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.FileProviders;

// ──────────────────────────────────────────────────────────────────────────────
// FitLife REST API — ASP.NET Core Minimal API
// All endpoints are defined inline below using MapGet/Post/Put/Delete.
// The API is consumed by:
//   • FitLife.Maui   — the mobile app for members and instructors
//   • FitLife.BlazorWebApp — connects directly to MySQL (not via this API)
// Database: MySQL via MySqlConnector (no ORM — raw SQL for full control).
// ──────────────────────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI metadata — Scalar UI available at /scalar in development
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "FitLife API";
        document.Info.Version = "v1";
        document.Info.Description =
            "REST API voor de FitLife fitnessapp, gebruikt door de MAUI mobiele app. " +
            "Geen JWT-authenticatie vereist — gebruik POST /auth/login voor gebruikersvalidatie.";
        return Task.CompletedTask;
    });
});

// Allow all origins/headers/methods so the MAUI app and any future web clients
// can call this API without CORS preflight failures.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFitLifeClients", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Scalar UI — browse all endpoints at /scalar in development.
    // To disable in production: keep this block development-only (already the case here).
    app.MapScalarApiReference(options =>
    {
        options.Title = "FitLife API";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve profile photos uploaded via POST /upload/photo/{userId} as static files
// under the /uploads URL path (e.g. http://localhost:8080/uploads/user_1_abc.jpg)
var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsDir);

// ── Database migrations ───────────────────────────────────────────────────────
// Run at startup to add missing columns/tables without a migration framework.
// Auto-migrate: add 'color' column to workouts if it does not exist
try
{
    var migConnStr = app.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(migConnStr))
    {
        await using var migConn = new MySqlConnection(migConnStr);
        await migConn.OpenAsync();
        const string checkCol = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='workouts' AND COLUMN_NAME='color'";
        await using var checkCmd = new MySqlCommand(checkCol, migConn);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0)
        {
            await using var addCmd = new MySqlCommand(
                "ALTER TABLE workouts ADD COLUMN color VARCHAR(7) NULL DEFAULT NULL;",
                migConn);
            await addCmd.ExecuteNonQueryAsync();
        }

        // Add 'is_archived' flag to lessons (used by LessonArchiveService in the Blazor app)
        const string checkArchived = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='lessons' AND COLUMN_NAME='is_archived'";
        await using var checkArchivedCmd = new MySqlCommand(checkArchived, migConn);
        if (Convert.ToInt32(await checkArchivedCmd.ExecuteScalarAsync()) == 0)
        {
            await using var addArchivedCmd = new MySqlCommand(
                "ALTER TABLE lessons ADD COLUMN is_archived TINYINT(1) NOT NULL DEFAULT 0;",
                migConn);
            await addArchivedCmd.ExecuteNonQueryAsync();
        }

        // Create the notifications table for persistent in-app notifications
        const string checkNotifTable = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='notifications'";
        await using var checkNotifCmd = new MySqlCommand(checkNotifTable, migConn);
        if (Convert.ToInt32(await checkNotifCmd.ExecuteScalarAsync()) == 0)
        {
            const string createNotif = """
                CREATE TABLE notifications (
                    id CHAR(36) NOT NULL PRIMARY KEY,
                    user_id INT NOT NULL,
                    title VARCHAR(255) NOT NULL,
                    message TEXT NOT NULL,
                    type INT NOT NULL DEFAULT 0,
                    is_read TINYINT(1) NOT NULL DEFAULT 0,
                    created_at DATETIME NOT NULL DEFAULT NOW(),
                    INDEX idx_notifications_user (user_id)
                );
                """;
            await using var createNotifCmd = new MySqlCommand(createNotif, migConn);
            await createNotifCmd.ExecuteNonQueryAsync();
        }

        // Track whether a credit was deducted when a reservation was created.
        // 1 = credit consumed (normal reserve flow), 0 = admin-added (no credit deducted).
        // DEFAULT 1 so existing reservations are treated as credit-consuming, preserving
        // the existing cancel-refund behaviour for pre-migration data.
        const string checkCreditUsed = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='reservations' AND COLUMN_NAME='credit_used'";
        await using var checkCuCmd = new MySqlCommand(checkCreditUsed, migConn);
        if (Convert.ToInt32(await checkCuCmd.ExecuteScalarAsync()) == 0)
        {
            await using var addCuCmd = new MySqlCommand(
                "ALTER TABLE reservations ADD COLUMN credit_used TINYINT(1) NOT NULL DEFAULT 1;",
                migConn);
            await addCuCmd.ExecuteNonQueryAsync();
        }

        // Create bike_reservations table for spinning lesson bike selection.
        // One row per (lesson, user) — UNIQUE on (lesson, row, bike) prevents double-booking a seat.
        const string checkBikeTable = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='bike_reservations'";
        await using var checkBikeTableCmd = new MySqlCommand(checkBikeTable, migConn);
        if (Convert.ToInt32(await checkBikeTableCmd.ExecuteScalarAsync()) == 0)
        {
            const string createBike = """
                CREATE TABLE bike_reservations (
                    id          INT AUTO_INCREMENT PRIMARY KEY,
                    lesson_id   INT NOT NULL,
                    user_id     INT NOT NULL,
                    row_number  INT NOT NULL,
                    bike_number INT NOT NULL,
                    created_at  DATETIME NOT NULL DEFAULT NOW(),
                    UNIQUE KEY unique_lesson_bike (lesson_id, row_number, bike_number),
                    UNIQUE KEY unique_lesson_user (lesson_id, user_id),
                    INDEX idx_bike_res_lesson (lesson_id),
                    INDEX idx_bike_res_user   (user_id)
                );
                """;
            await using var createBikeCmd = new MySqlCommand(createBike, migConn);
            await createBikeCmd.ExecuteNonQueryAsync();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Migration] color kolom: {ex.Message}");
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

// ── HTTP pipeline ─────────────────────────────────────────────────────────────
app.UseCors("AllowFitLifeClients");

// ── Endpoints ─────────────────────────────────────────────────────────────────

// GET /workouts — Returns all workout types as id+name pairs for dropdown selectors
// Used by the MAUI ManageLessonPage to populate the workout picker
app.MapGet("/workouts", async (IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    var items = new List<DropdownItemDto>();
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = "SELECT id, name, COALESCE(color, '#5B6636') AS color FROM workouts ORDER BY name";
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        items.Add(new DropdownItemDto
        {
            Id    = reader.GetInt32("id"),
            Name  = reader.GetString("name"),
            Color = reader.GetString("color")
        });

    return Results.Ok(items);
})
.WithName("GetWorkouts")
.WithTags("Referentiedata")
.WithSummary("Haal alle werkvormen op")
.Produces<IEnumerable<DropdownItemDto>>(200);

// GET /locations — Returns all locations/halls as id+name pairs for dropdown selectors
app.MapGet("/locations", async (IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    var items = new List<DropdownItemDto>();
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = "SELECT id, name FROM locations ORDER BY name";
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        items.Add(new DropdownItemDto { Id = reader.GetInt32("id"), Name = reader.GetString("name") });

    return Results.Ok(items);
})
.WithName("GetLocations")
.WithTags("Referentiedata")
.WithSummary("Haal alle locaties/zalen op")
.Produces<IEnumerable<DropdownItemDto>>(200);

// GET /instructors — Returns all instructors (role='instructor') as id+name pairs
app.MapGet("/instructors", async (IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    var items = new List<DropdownItemDto>();
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = "SELECT id, display_name AS name FROM users WHERE role = 'instructor' ORDER BY display_name";
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        items.Add(new DropdownItemDto
        {
            Id   = reader.GetInt32("id"),
            Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "Onbekend" : reader.GetString("name")
        });

    return Results.Ok(items);
})
.WithName("GetInstructors")
.WithTags("Referentiedata")
.WithSummary("Haal alle instructeurs op")
.Produces<IEnumerable<DropdownItemDto>>(200);

// GET /lessons/instructor/{instructorId} — All lessons where the given user is the instructor.
// Used by the MAUI InstructorLessonsPage to show a trainer their own schedule.
app.MapGet("/lessons/instructor/{instructorId:int}", async (int instructorId, IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    var lessons = new List<LessonResponse>();
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    try { if (Convert.ToInt32(await new MySqlCommand("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='workouts' AND COLUMN_NAME='color'", connection).ExecuteScalarAsync()) == 0) { await new MySqlCommand("ALTER TABLE workouts ADD COLUMN color VARCHAR(7) NULL DEFAULT NULL;", connection).ExecuteNonQueryAsync(); } } catch { }

    const string sql = """
        SELECT
            l.id,
            l.start_time,
            DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
            COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
            l.workout_id,
            w.name AS workout_name,
            COALESCE(w.color, '#5B6636') AS workout_color,
            l.instructor_id,
            u.display_name AS instructor_name,
            l.location_id,
            loc.name AS location_name,
            COALESCE(r_counts.cnt, 0) AS current_participants,
            COALESCE(w_counts.cnt, 0) AS waitlist_count
        FROM lessons l
        INNER JOIN workouts w ON w.id = l.workout_id
        LEFT JOIN users u ON u.id = l.instructor_id
        INNER JOIN locations loc ON loc.id = l.location_id
        LEFT JOIN (
            SELECT lesson_id, COUNT(*) AS cnt
            FROM reservations WHERE is_cancelled = 0
            GROUP BY lesson_id
        ) r_counts ON r_counts.lesson_id = l.id
        LEFT JOIN (
            SELECT lesson_id, COUNT(*) AS cnt
            FROM waitlist_entries
            GROUP BY lesson_id
        ) w_counts ON w_counts.lesson_id = l.id
        WHERE l.instructor_id = @instructorId
        ORDER BY l.start_time;
        """;

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@instructorId", instructorId);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        lessons.Add(new LessonResponse
        {
            Id = reader.GetInt32("id"),
            StartTime = reader.GetDateTime("start_time"),
            EndTime = reader.GetDateTime("end_time"),
            MaxParticipants = reader.GetInt32("max_participants"),
            WorkoutId = reader.GetInt32("workout_id"),
            WorkoutName = reader.GetString("workout_name"),
            WorkoutColor = reader.GetString("workout_color"),
            InstructorId = instructorId,
            InstructorName = reader.IsDBNull(reader.GetOrdinal("instructor_name")) ? "Onbekende instructeur" : reader.GetString("instructor_name"),
            LocationId = reader.GetInt32("location_id"),
            LocationName = reader.GetString("location_name"),
            CurrentParticipantCount = reader.GetInt32("current_participants"),
            WaitlistCount = reader.GetInt32("waitlist_count")
        });
    }

    return Results.Ok(lessons);
})
.WithName("GetInstructorLessons")
.WithTags("Lessen")
.WithSummary("Haal het lesrooster van een instructeur op")
.Produces<IEnumerable<LessonResponse>>(200);

// POST /lessons — Creates a new lesson. Body: LessonSaveDto. Returns the new lesson ID.
app.MapPost("/lessons", async (IConfiguration configuration, LessonSaveDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Calculate duration in minutes
        var duration = (int)(request.EndTime - request.StartTime).TotalMinutes;
        if (duration <= 0) duration = 60;

        const string sql = """
            INSERT INTO lessons (start_time, duration_minutes, capacity_override, workout_id, instructor_id, location_id)
            VALUES (@startTime, @duration, @capacity, @workoutId, @instructorId, @locationId);
            SELECT LAST_INSERT_ID();
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@startTime", request.StartTime);
        command.Parameters.AddWithValue("@duration", duration);
        command.Parameters.AddWithValue("@capacity", request.MaxParticipants > 0 ? request.MaxParticipants : DBNull.Value);
        command.Parameters.AddWithValue("@workoutId", request.WorkoutId);
        command.Parameters.AddWithValue("@instructorId", request.InstructorId);
        command.Parameters.AddWithValue("@locationId", request.LocationId);

        var newId = await command.ExecuteScalarAsync();
        return Results.Ok(new { success = true, id = newId, message = "Les succesvol aangemaakt." });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error creating lesson: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het aanmaken van de les.");
    }
})
.WithName("CreateLesson")
.WithTags("Lessen")
.WithSummary("Maak een nieuwe les aan")
.Produces<object>(200);

// PUT /lessons/{lessonId} — Updates all editable fields of an existing lesson.
app.MapPut("/lessons/{lessonId:int}", async (int lessonId, IConfiguration configuration, LessonSaveDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var duration = (int)(request.EndTime - request.StartTime).TotalMinutes;
        if (duration <= 0) duration = 60;

        const string sql = """
            UPDATE lessons SET
                start_time = @startTime,
                duration_minutes = @duration,
                capacity_override = @capacity,
                workout_id = @workoutId,
                instructor_id = @instructorId,
                location_id = @locationId
            WHERE id = @lessonId;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@startTime", request.StartTime);
        command.Parameters.AddWithValue("@duration", duration);
        command.Parameters.AddWithValue("@capacity", request.MaxParticipants > 0 ? request.MaxParticipants : DBNull.Value);
        command.Parameters.AddWithValue("@workoutId", request.WorkoutId);
        command.Parameters.AddWithValue("@instructorId", request.InstructorId);
        command.Parameters.AddWithValue("@locationId", request.LocationId);
        command.Parameters.AddWithValue("@lessonId", lessonId);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0
            ? Results.Ok(new { success = true, message = "Les succesvol bijgewerkt." })
            : Results.Ok(new { success = false, message = "Les niet gevonden." });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error updating lesson: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het bijwerken van de les.");
    }
})
.WithName("UpdateLesson")
.WithTags("Lessen")
.WithSummary("Werk een bestaande les bij")
.Produces<object>(200);

// DELETE /lessons/{lessonId} — Deletes a lesson. Rejected if active reservations exist.
app.MapDelete("/lessons/{lessonId:int}", async (int lessonId, IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Check if there are active reservations
        const string checkSql = "SELECT COUNT(*) FROM reservations WHERE lesson_id = @lessonId AND is_cancelled = 0";
        await using var checkCmd = new MySqlCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@lessonId", lessonId);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (count > 0)
            return Results.Ok(new { success = false, message = $"Kan les niet verwijderen: er zijn nog {count} actieve reserveringen." });

        const string sql = "DELETE FROM lessons WHERE id = @lessonId";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@lessonId", lessonId);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0
            ? Results.Ok(new { success = true, message = "Les succesvol verwijderd." })
            : Results.Ok(new { success = false, message = "Les niet gevonden." });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error deleting lesson: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het verwijderen van de les.");
    }
})
.WithName("DeleteLesson")
.WithTags("Lessen")
.WithSummary("Verwijder een les (geblokkeerd als er actieve reserveringen zijn)")
.Produces<object>(200);

// POST /lessons/{lessonId}/add-member — Admin-only: adds a member to a lesson
// without deducting a credit. Checks capacity before inserting the reservation.
app.MapPost("/lessons/{lessonId:int}/add-member", async (int lessonId, IConfiguration configuration, AddMemberDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Check capacity
        const string capacitySql = """
            SELECT COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_capacity,
                   (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_count
            FROM lessons l
            INNER JOIN workouts w ON w.id = l.workout_id
            INNER JOIN locations loc ON loc.id = l.location_id
            WHERE l.id = @lessonId
            """;

        await using var capacityCmd = new MySqlCommand(capacitySql, connection);
        capacityCmd.Parameters.AddWithValue("@lessonId", lessonId);
        await using var reader = await capacityCmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            await reader.CloseAsync();
            return Results.Ok(new { success = false, message = "Les niet gevonden." });
        }

        int maxCapacity = reader.GetInt32("max_capacity");
        int currentCount = reader.GetInt32("current_count");
        await reader.CloseAsync();

        if (currentCount >= maxCapacity)
            return Results.Ok(new { success = false, message = "Les is vol." });

        // Check for existing reservation
        const string existingSql = "SELECT id FROM reservations WHERE lesson_id = @lessonId AND member_id = @userId AND is_cancelled = 0";
        await using var existingCmd = new MySqlCommand(existingSql, connection);
        existingCmd.Parameters.AddWithValue("@lessonId", lessonId);
        existingCmd.Parameters.AddWithValue("@userId", request.UserId);
        var existing = await existingCmd.ExecuteScalarAsync();
        if (existing != null)
            return Results.Ok(new { success = false, message = "Dit lid is al aangemeld voor deze les." });

        // Insert reservation without credit deduction — credit_used=0 so a cancel never refunds
        const string insertSql = """
            INSERT INTO reservations (lesson_id, member_id, reservation_date, is_cancelled, credit_used)
            VALUES (@lessonId, @userId, NOW(), 0, 0)
            """;
        await using var insertCmd = new MySqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@lessonId", lessonId);
        insertCmd.Parameters.AddWithValue("@userId", request.UserId);
        await insertCmd.ExecuteNonQueryAsync();

        return Results.Ok(new { success = true, message = "Lid succesvol toegevoegd aan de les." });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error adding member: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het toevoegen van het lid.");
    }
})
.WithName("AddMemberToLesson")
.WithTags("Lessen")
.WithSummary("Voeg een lid admin-zijdig toe aan een les (zonder credit)")
.Produces<object>(200);

// GET /lessons?userId={id}&from={iso}&to={iso} — Returns lessons filtered by date range.
// from/to are optional ISO-8601 strings; omit for no date filter (e.g. list view).
// Participant counts use pre-aggregated JOINs instead of correlated subqueries for performance.
app.MapGet("/lessons", async (IConfiguration configuration, int? userId, DateTime? from, DateTime? to) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    var lessons = new List<LessonResponse>();

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    try {
        if (Convert.ToInt32(await new MySqlCommand("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='workouts' AND COLUMN_NAME='color'", connection).ExecuteScalarAsync()) == 0)
            await new MySqlCommand("ALTER TABLE workouts ADD COLUMN color VARCHAR(7) NULL DEFAULT NULL;", connection).ExecuteNonQueryAsync();
        if (Convert.ToInt32(await new MySqlCommand("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='lessons' AND COLUMN_NAME='is_archived'", connection).ExecuteScalarAsync()) == 0)
            await new MySqlCommand("ALTER TABLE lessons ADD COLUMN is_archived TINYINT(1) NOT NULL DEFAULT 0;", connection).ExecuteNonQueryAsync();
    } catch { }

    // Aggregate joins replace correlated subqueries — single pass over reservations/waitlist tables.
    // is_booked uses a correlated EXISTS which MySQL resolves cheaply via the reservation index.
    const string sql = """
        SELECT
            l.id,
            l.start_time,
            DATE_ADD(l.start_time, INTERVAL l.duration_minutes MINUTE) AS end_time,
            COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_participants,
            l.workout_id,
            w.name AS workout_name,
            COALESCE(w.color, '#5B6636') AS workout_color,
            l.instructor_id,
            u.display_name AS instructor_name,
            l.location_id,
            loc.name AS location_name,
            COALESCE(r_counts.cnt, 0) AS current_participants,
            COALESCE(w_counts.cnt, 0) AS waitlist_count,
            CASE WHEN @userId > 0 AND EXISTS (
                SELECT 1 FROM reservations rb
                WHERE rb.lesson_id = l.id AND rb.member_id = @userId AND rb.is_cancelled = 0
            ) THEN 1 ELSE 0 END AS is_booked
        FROM lessons l
        INNER JOIN workouts w ON w.id = l.workout_id
        LEFT JOIN users u ON u.id = l.instructor_id
        INNER JOIN locations loc ON loc.id = l.location_id
        LEFT JOIN (
            SELECT lesson_id, COUNT(*) AS cnt
            FROM reservations WHERE is_cancelled = 0
            GROUP BY lesson_id
        ) r_counts ON r_counts.lesson_id = l.id
        LEFT JOIN (
            SELECT lesson_id, COUNT(*) AS cnt
            FROM waitlist_entries
            GROUP BY lesson_id
        ) w_counts ON w_counts.lesson_id = l.id
        WHERE (@from IS NULL OR l.start_time >= @from)
          AND (@to IS NULL OR l.start_time < @to)
          AND COALESCE(l.is_archived, 0) = 0
        ORDER BY l.start_time;
        """;

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@userId", userId.HasValue && userId.Value > 0 ? userId.Value : 0);
    command.Parameters.AddWithValue("@from", from.HasValue ? (object)from.Value : DBNull.Value);
    command.Parameters.AddWithValue("@to",   to.HasValue   ? (object)to.Value   : DBNull.Value);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var lessonId = reader.GetInt32("id");
        var maxParticipants = reader.GetInt32("max_participants");
        bool isBooked = reader.GetInt32("is_booked") == 1;
        int currentCount = reader.GetInt32("current_participants");
        int waitlistCount = reader.GetInt32("waitlist_count");

        lessons.Add(new LessonResponse
        {
            Id = lessonId,
            StartTime = reader.GetDateTime("start_time"),
            EndTime = reader.GetDateTime("end_time"),
            MaxParticipants = maxParticipants,
            WorkoutId = reader.GetInt32("workout_id"),
            WorkoutName = reader.GetString("workout_name"),
            WorkoutColor = reader.GetString("workout_color"),
            InstructorId = reader.IsDBNull(reader.GetOrdinal("instructor_id")) ? 0 : reader.GetInt32("instructor_id"),
            InstructorName = reader.IsDBNull(reader.GetOrdinal("instructor_name")) ? "Onbekende instructeur" : reader.GetString("instructor_name"),
            LocationId = reader.GetInt32("location_id"),
            LocationName = reader.GetString("location_name"),
            CurrentParticipantCount = currentCount,
            WaitlistCount = waitlistCount,
            IsBooked = isBooked
        });
    }

    return Results.Ok(lessons);
})
.WithName("GetLessons")
.WithTags("Lessen")
.WithSummary("Haal lessen op, optioneel gefilterd op gebruiker (userId) en datumbereik (from/to)")
.Produces<IEnumerable<LessonResponse>>(200);

// GET /lessons/{lessonId}/participants — Returns all non-cancelled members enrolled in a lesson.
app.MapGet("/lessons/{lessonId:int}/participants", async (int lessonId, IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    var participants = new List<ParticipantResponse>();

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = """
        SELECT
            u.id AS member_id,
            u.display_name AS member_name,
            u.photo_url AS image_url
        FROM reservations r
        INNER JOIN users u ON u.id = r.member_id
        WHERE r.lesson_id = @lessonId
          AND r.is_cancelled = 0
          AND u.role = 'member'
        ORDER BY r.reservation_date;
        """;

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@lessonId", lessonId);

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        participants.Add(new ParticipantResponse
        {
            MemberId = reader.GetInt32("member_id"),
            Name = reader.GetString("member_name"),
            ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url"))
                ? null
                : reader.GetString("image_url"),
            IsBuddy = false
        });
    }

    return Results.Ok(participants);
})
.WithName("GetLessonParticipants")
.WithTags("Lessen")
.WithSummary("Haal actieve deelnemers van een les op")
.Produces<IEnumerable<ParticipantResponse>>(200);

// GET /lessons/{lessonId}/waitlist — Returns all members on the waitlist for a lesson, ordered by position.
app.MapGet("/lessons/{lessonId:int}/waitlist", async (int lessonId, IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    var waitlist = new List<ParticipantResponse>();

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = """
        SELECT
            u.id AS member_id,
            u.display_name AS member_name,
            u.photo_url AS image_url
        FROM waitlist_entries we
        INNER JOIN users u ON u.id = we.member_id
        WHERE we.lesson_id = @lessonId
        ORDER BY we.position;
        """;

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@lessonId", lessonId);

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        waitlist.Add(new ParticipantResponse
        {
            MemberId = reader.GetInt32("member_id"),
            Name = reader.IsDBNull(reader.GetOrdinal("member_name"))
                ? "Onbekend"
                : reader.GetString("member_name"),
            ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url"))
                ? null
                : reader.GetString("image_url"),
            IsBuddy = false
        });
    }

    return Results.Ok(waitlist);
})
.WithName("GetLessonWaitlist")
.WithTags("Lessen")
.WithSummary("Haal de wachtlijst van een les op (geordend op positie)")
.Produces<IEnumerable<ParticipantResponse>>(200);

// POST /lessons/{lessonId}/waitlist — Adds the user to the waitlist for a full lesson.
// Checks for duplicate entries; returns the user's position on the waitlist.
app.MapPost("/lessons/{lessonId:int}/waitlist", async (int lessonId, IConfiguration configuration, ReservationRequestDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Reject if already on waitlist
        const string checkSql = "SELECT id FROM waitlist_entries WHERE lesson_id = @lessonId AND member_id = @userId";
        await using var checkCmd = new MySqlCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@lessonId", lessonId);
        checkCmd.Parameters.AddWithValue("@userId", request.UserId);
        var existing = await checkCmd.ExecuteScalarAsync();
        if (existing != null)
            return Results.Ok(new { success = false, alreadyOnWaitlist = true, message = "Je staat al op de wachtlijst voor deze les." });

        // Determine next position and insert
        const string insertSql = """
            INSERT INTO waitlist_entries (lesson_id, member_id, position)
            SELECT @lessonId, @userId, COALESCE(MAX(wt.position), 0) + 1
            FROM waitlist_entries wt
            WHERE wt.lesson_id = @lessonId
            """;
        await using var insertCmd = new MySqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@lessonId", lessonId);
        insertCmd.Parameters.AddWithValue("@userId", request.UserId);
        await insertCmd.ExecuteNonQueryAsync();

        // Return the user's position
        const string posSql = "SELECT position FROM waitlist_entries WHERE lesson_id = @lessonId AND member_id = @userId";
        await using var posCmd = new MySqlCommand(posSql, connection);
        posCmd.Parameters.AddWithValue("@lessonId", lessonId);
        posCmd.Parameters.AddWithValue("@userId", request.UserId);
        var position = Convert.ToInt32(await posCmd.ExecuteScalarAsync());

        return Results.Ok(new { success = true, position, message = "Je bent toegevoegd aan de wachtlijst." });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error joining waitlist: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het aanmelden voor de wachtlijst.");
    }
})
.WithName("JoinWaitlist")
.WithTags("Lessen")
.WithSummary("Schrijf een gebruiker in op de wachtlijst van een volle les")
.Produces<object>(200);

// POST /auth/login — Verifies email + password against the database and returns
// the full user profile (id, name, role, credits, subscription) on success.
// Password is stored as SHA-256 hex; plaintext is also accepted for legacy seeds.
app.MapPost("/auth/login", async (IConfiguration configuration, LoginRequestDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.Ok(new LoginResponseDto
        {
            Success = false,
            Message = "Email en wachtwoord zijn verplicht."
        });
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Query to get user by email
        const string sql = """
            SELECT id, email, password_hash, display_name, photo_url, role, credits, subscription_type, subscription_renewal_date
            FROM users
            WHERE email = @email
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", request.Email);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var userId = reader.GetInt32("id");
            var email = reader.GetString("email");
            var passwordHash = reader.GetString("password_hash");
            var displayName = reader.IsDBNull(reader.GetOrdinal("display_name"))
                ? email.Split('@')[0]
                : reader.GetString("display_name");
            var photoUrl = reader.IsDBNull(reader.GetOrdinal("photo_url"))
                ? null
                : reader.GetString("photo_url");
            var role = reader.GetString("role");
            var credits = reader.IsDBNull(reader.GetOrdinal("credits")) ? 0 : reader.GetInt32("credits");
            var subscriptionType = reader.IsDBNull(reader.GetOrdinal("subscription_type"))
                ? null
                : reader.GetString("subscription_type");
            var subscriptionRenewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date"))
                ? null
                : reader.GetDateTime("subscription_renewal_date").ToString("yyyy-MM-dd");

            // Verify password (using bcrypt-like hash verification)
            // For demo purposes, we check if the password matches the hash
            // In production, use proper password hashing (BCrypt.Net, etc.)
            string computedHash = ComputeSha256Hash(request.Password);

            if (passwordHash == computedHash || passwordHash == request.Password)
            {
                // Login successful
                return Results.Ok(new LoginResponseDto
                {
                    Success = true,
                    Message = "Login succesvol.",
                    UserId = userId,
                    DisplayName = displayName,
                    PhotoUrl = photoUrl,
                    Email = email,
                    Role = role,
                    Credits = credits,
                    SubscriptionType = subscriptionType,
                    SubscriptionRenewalDate = subscriptionRenewalDate
                });
            }
            else
            {
                // Invalid password
                return Results.Ok(new LoginResponseDto
                {
                    Success = false,
                    Message = "Ongeldige inloggegevens."
                });
            }
        }
        else
        {
            // User not found
            return Results.Ok(new LoginResponseDto
            {
                Success = false,
                Message = "Ongeldige inloggegevens."
            });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error during login: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het inloggen.");
    }
})
.WithName("Login")
.WithTags("Authenticatie")
.WithSummary("Inloggen met email en wachtwoord")
.Produces<LoginResponseDto>(200);

// POST /lessons/{lessonId}/reserve — Creates a reservation for the given user.
// Checks: user has ≥1 credit, lesson exists and has capacity, not already reserved.
// Uses a transaction so credit deduction and reservation insert are atomic.
app.MapPost("/lessons/{lessonId:int}/reserve", async (int lessonId, IConfiguration configuration, ReservationRequestDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Check if user has enough credits
            const string checkCreditsSql = "SELECT credits FROM users WHERE id = @userId FOR UPDATE";
            await using var checkCmd = new MySqlCommand(checkCreditsSql, connection, transaction);
            checkCmd.Parameters.AddWithValue("@userId", request.UserId);
            
            var creditsObj = await checkCmd.ExecuteScalarAsync();
            if (creditsObj == null || creditsObj == DBNull.Value)
            {
                await transaction.RollbackAsync();
                return Results.Ok(new { success = false, message = "Gebruiker niet gevonden." });
            }

            int currentCredits = Convert.ToInt32(creditsObj);
            if (currentCredits < 1)
            {
                await transaction.RollbackAsync();
                return Results.Ok(new { success = false, message = "Onvoldoende credits. Je hebt minimaal 1 credit nodig om een les te reserveren." });
            }

            // Check lesson capacity
            const string checkCapacitySql = """
                SELECT 
                    COALESCE(l.capacity_override, w.default_capacity, loc.capacity, 0) AS max_capacity,
                    (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_count
                FROM lessons l
                INNER JOIN workouts w ON w.id = l.workout_id
                INNER JOIN locations loc ON loc.id = l.location_id
                WHERE l.id = @lessonId
                """;
            
            await using var capacityCmd = new MySqlCommand(checkCapacitySql, connection, transaction);
            capacityCmd.Parameters.AddWithValue("@lessonId", lessonId);
            await using var reader = await capacityCmd.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
            {
                await reader.CloseAsync();
                await transaction.RollbackAsync();
                return Results.Ok(new { success = false, message = "Les niet gevonden." });
            }

            int maxCapacity = reader.GetInt32("max_capacity");
            int currentCount = reader.GetInt32("current_count");
            await reader.CloseAsync();

            if (currentCount >= maxCapacity)
            {
                await transaction.RollbackAsync();
                return Results.Ok(new { success = false, lessonFull = true, message = "Les is vol. Je kunt je aanmelden voor de wachtlijst." });
            }

            // Check if user already has a reservation
            const string checkExistingSql = "SELECT id FROM reservations WHERE lesson_id = @lessonId AND member_id = @userId AND is_cancelled = 0";
            await using var existingCmd = new MySqlCommand(checkExistingSql, connection, transaction);
            existingCmd.Parameters.AddWithValue("@lessonId", lessonId);
            existingCmd.Parameters.AddWithValue("@userId", request.UserId);
            
            var existingReservation = await existingCmd.ExecuteScalarAsync();
            if (existingReservation != null)
            {
                await transaction.RollbackAsync();
                return Results.Ok(new { success = false, message = "Je bent al aangemeld voor deze les." });
            }

            // Create reservation — credit_used=1 so a cancel later refunds exactly 1 credit
            const string insertReservationSql = """
                INSERT INTO reservations (lesson_id, member_id, reservation_date, is_cancelled, credit_used)
                VALUES (@lessonId, @userId, NOW(), 0, 1)
                """;
            
            await using var insertCmd = new MySqlCommand(insertReservationSql, connection, transaction);
            insertCmd.Parameters.AddWithValue("@lessonId", lessonId);
            insertCmd.Parameters.AddWithValue("@userId", request.UserId);
            await insertCmd.ExecuteNonQueryAsync();

            // Deduct 1 credit from user
            const string deductCreditSql = "UPDATE users SET credits = credits - 1 WHERE id = @userId";
            await using var deductCmd = new MySqlCommand(deductCreditSql, connection, transaction);
            deductCmd.Parameters.AddWithValue("@userId", request.UserId);
            await deductCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return Results.Ok(new { success = true, message = "Reservering succesvol! 1 credit is afgeschreven.", remainingCredits = currentCredits - 1 });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Diagnostics.Debug.WriteLine($"Error during reservation: {ex.Message}");
            return Results.Problem("Er is een fout opgetreden bij het reserveren.");
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error connecting to database: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het reserveren.");
    }
})
.WithName("ReserveLesson")
.WithTags("Reserveringen")
.WithSummary("Reserveer een les voor een gebruiker (trekt 1 credit af, transactioneel)")
.Produces<object>(200);

// DELETE /lessons/{lessonId}/cancel?userId={userId} — Cancels the user's reservation
// and refunds 1 credit. Uses a transaction so the cancellation and credit refund are atomic.
app.MapDelete("/lessons/{lessonId:int}/cancel", async (int lessonId, IConfiguration configuration, int userId) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Fetch reservation id, credit_used flag, and the user's subscription type in one go
            const string checkSql = """
                SELECT r.id, r.credit_used, u.subscription_type
                FROM reservations r
                JOIN users u ON u.id = r.member_id
                WHERE r.lesson_id = @lessonId AND r.member_id = @userId AND r.is_cancelled = 0
                """;
            await using var checkCmd = new MySqlCommand(checkSql, connection, transaction);
            checkCmd.Parameters.AddWithValue("@lessonId", lessonId);
            checkCmd.Parameters.AddWithValue("@userId", userId);

            object? reservationId = null;
            bool creditUsed = false;
            string subscriptionType = "Rookie";

            await using (var rdr = await checkCmd.ExecuteReaderAsync())
            {
                if (!await rdr.ReadAsync())
                {
                    await rdr.CloseAsync();
                    await transaction.RollbackAsync();
                    return Results.Ok(new { success = false, message = "Geen actieve reservering gevonden voor deze les." });
                }
                reservationId    = rdr.GetValue(0);
                creditUsed       = rdr.GetInt32(1) == 1;
                subscriptionType = rdr.GetString(2);
            }

            // Cancel reservation
            const string cancelSql = "UPDATE reservations SET is_cancelled = 1 WHERE id = @reservationId";
            await using var cancelCmd = new MySqlCommand(cancelSql, connection, transaction);
            cancelCmd.Parameters.AddWithValue("@reservationId", reservationId);
            await cancelCmd.ExecuteNonQueryAsync();

            // Release bike reservation (spinning lessons) — harmless no-op for other lesson types
            const string releaseBikeSql = "DELETE FROM bike_reservations WHERE lesson_id = @lessonId AND user_id = @userId";
            await using var releaseBikeCmd = new MySqlCommand(releaseBikeSql, connection, transaction);
            releaseBikeCmd.Parameters.AddWithValue("@lessonId", lessonId);
            releaseBikeCmd.Parameters.AddWithValue("@userId", userId);
            await releaseBikeCmd.ExecuteNonQueryAsync();

            int updatedCredits;
            string responseMessage;

            if (creditUsed)
            {
                // Cap refund at the subscription maximum so credits never exceed the plan limit.
                // Advanced uses 999 as an unlimited sentinel — capping there keeps it at 999.
                int maxCredits = subscriptionType switch
                {
                    "Rookie"       => 9,
                    "Intermediate" => 13,
                    "Advanced"     => 999,
                    _              => 9
                };

                const string refundSql = "UPDATE users SET credits = LEAST(credits + 1, @maxCredits) WHERE id = @userId";
                await using var refundCmd = new MySqlCommand(refundSql, connection, transaction);
                refundCmd.Parameters.AddWithValue("@maxCredits", maxCredits);
                refundCmd.Parameters.AddWithValue("@userId", userId);
                await refundCmd.ExecuteNonQueryAsync();

                const string getCreditsSql = "SELECT credits FROM users WHERE id = @userId";
                await using var getCreditsCmd = new MySqlCommand(getCreditsSql, connection, transaction);
                getCreditsCmd.Parameters.AddWithValue("@userId", userId);
                updatedCredits  = Convert.ToInt32(await getCreditsCmd.ExecuteScalarAsync());
                responseMessage = "Reservering geannuleerd! 1 credit is teruggegeven.";
            }
            else
            {
                // Reservation was admin-added (credit_used=0) — no credit to refund
                const string getCreditsSql = "SELECT credits FROM users WHERE id = @userId";
                await using var getCreditsCmd = new MySqlCommand(getCreditsSql, connection, transaction);
                getCreditsCmd.Parameters.AddWithValue("@userId", userId);
                updatedCredits  = Convert.ToInt32(await getCreditsCmd.ExecuteScalarAsync());
                responseMessage = "Reservering geannuleerd.";
            }

            await transaction.CommitAsync();

            return Results.Ok(new { success = true, message = responseMessage, remainingCredits = updatedCredits });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Diagnostics.Debug.WriteLine($"Error during cancellation: {ex.Message}");
            return Results.Problem("Er is een fout opgetreden bij het annuleren.");
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error connecting to database: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het annuleren.");
    }
})
.WithName("CancelReservation")
.WithTags("Reserveringen")
.WithSummary("Annuleer een reservering en restitueer 1 credit indien van toepassing (transactioneel)")
.Produces<object>(200);

// Helper: computes a lowercase hex SHA-256 hash of the input string.
// Used to verify passwords stored in the database.
// Note: for production use BCrypt or Argon2 instead of plain SHA-256.
static string ComputeSha256Hash(string rawData)
{
    using (SHA256 sha256Hash = SHA256.Create())
    {
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}

// GET /users/{userId}/lessons — Returns all non-cancelled reservations for the user,
// joined with lesson, workout, instructor, and location data for display in MyLessonsPage.
app.MapGet("/users/{userId:int}/lessons", async (int userId, IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    var lessons = new List<UserLessonDto>();

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = """
        SELECT
            l.id,
            l.start_time,
            w.name  AS workout_name,
            u.display_name AS instructor_name,
            loc.name AS location_name
        FROM reservations r
        INNER JOIN lessons l   ON l.id  = r.lesson_id
        INNER JOIN workouts w  ON w.id  = l.workout_id
        LEFT  JOIN users u     ON u.id  = l.instructor_id
        INNER JOIN locations loc ON loc.id = l.location_id
        WHERE r.member_id = @userId AND r.is_cancelled = 0
        ORDER BY l.start_time;
        """;

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@userId", userId);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        lessons.Add(new UserLessonDto
        {
            Id           = reader.GetInt32("id"),
            WorkoutName  = reader.GetString("workout_name"),
            StartTime    = reader.GetDateTime("start_time"),
            InstructorName = reader.IsDBNull(reader.GetOrdinal("instructor_name"))
                ? "Onbekende instructeur"
                : reader.GetString("instructor_name"),
            LocationName = reader.GetString("location_name")
        });
    }

    return Results.Ok(lessons);
})
.WithName("GetUserLessons")
.WithTags("Gebruikers")
.WithSummary("Haal alle actieve reserveringen van een gebruiker op")
.Produces<IEnumerable<UserLessonDto>>(200);

// ── Subscription endpoints ────────────────────────────────────────────────────

// GET /subscriptions/plans — Returns the three fixed subscription tiers
// (Rookie, Intermediate, Advanced) with monthly/yearly pricing and credit counts.
app.MapGet("/subscriptions/plans", () =>
{
    var plans = new List<SubscriptionPlanDto>
    {
        new SubscriptionPlanDto
        {
            Name = "Rookie",
            MonthlyPrice = 45m,
            YearlyPrice = 459m,
            Credits = 9,
            IsUnlimited = false,
            Description = "Ideaal voor beginners"
        },
        new SubscriptionPlanDto
        {
            Name = "Intermediate",
            MonthlyPrice = 65m,
            YearlyPrice = 663m,
            Credits = 13,
            IsUnlimited = false,
            Description = "Perfecte balans"
        },
        new SubscriptionPlanDto
        {
            Name = "Advanced",
            MonthlyPrice = 85m,
            YearlyPrice = 867m,
            Credits = 0,
            IsUnlimited = true,
            Description = "Voor de fanatieke sporter"
        }
    };

    return Results.Ok(plans);
})
.WithName("GetSubscriptionPlans")
.WithTags("Abonnementen")
.WithSummary("Haal de beschikbare abonnementsplannen op (Rookie, Intermediate, Advanced)")
.Produces<IEnumerable<SubscriptionPlanDto>>(200);

// POST /subscriptions/change — Changes the user's subscription type and resets their
// credits to match the new plan. Applied immediately (not deferred to the renewal date).
app.MapPost("/subscriptions/change", async (IConfiguration configuration, SubscriptionChangeRequestDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.NewSubscriptionType))
    {
        return Results.Ok(new SubscriptionChangeResponseDto
        {
            Success = false,
            Message = "Gebruiker ID en nieuw abonnementstype zijn verplicht."
        });
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // First, get current subscription info
        const string selectSql = """
            SELECT subscription_type, subscription_renewal_date
            FROM users
            WHERE id = @userId
            LIMIT 1;
            """;

        await using var selectCommand = new MySqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@userId", request.UserId);

        await using var reader = await selectCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Gebruiker niet gevonden."
            });
        }

        var currentSubscription = reader.IsDBNull(reader.GetOrdinal("subscription_type"))
            ? null
            : reader.GetString("subscription_type");
        var renewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date"))
            ? DateTime.Now.AddMonths(1)
            : reader.GetDateTime("subscription_renewal_date");

        await reader.CloseAsync();

        var newCredits = request.NewSubscriptionType switch
        {
            "Rookie"       => 9,
            "Intermediate" => 13,
            "Advanced"     => 999,
            _              => 0
        };

        const string updateSql = """
            UPDATE users
            SET subscription_type = @newSubscriptionType,
                subscription_renewal_date = @renewalDate,
                credits = @credits
            WHERE id = @userId;
            """;

        await using var updateCommand = new MySqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@newSubscriptionType", request.NewSubscriptionType);
        updateCommand.Parameters.AddWithValue("@renewalDate", renewalDate.Date);
        updateCommand.Parameters.AddWithValue("@credits", newCredits);
        updateCommand.Parameters.AddWithValue("@userId", request.UserId);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = true,
                Message = $"Je abonnement is gewijzigd naar {request.NewSubscriptionType}.",
                EffectiveDate = renewalDate.ToString("yyyy-MM-dd"),
                NewSubscriptionType = request.NewSubscriptionType,
                BillingCycle = request.IsYearly ? "yearly" : "monthly"
            });
        }
        else
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Abonnementswijziging kon niet worden opgeslagen."
            });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error changing subscription: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het wijzigen van het abonnement.");
    }
})
.WithName("ChangeSubscription")
.WithTags("Abonnementen")
.WithSummary("Wijzig het abonnementstype van een gebruiker (credits worden direct gereset)")
.Produces<SubscriptionChangeResponseDto>(200);

// GET /subscriptions/status/{userId} — Returns the user's current subscription type,
// renewal date, and credit balance. Used by ProfileViewModel and SubscriptionViewModel.
// If the stored renewal date is in the past, it is automatically advanced by 28-day
// cycles until it lies in the future; credits are reset to the plan maximum for each
// cycle that passed, and the new values are persisted to the database.
app.MapGet("/subscriptions/status/{userId}", async (IConfiguration configuration, int userId) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT subscription_type, subscription_renewal_date, credits
            FROM users
            WHERE id = @userId
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);

        string? subscriptionType;
        DateTime? rawRenewalDate;
        int credits;

        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync())
                return Results.NotFound();

            subscriptionType = reader.IsDBNull(reader.GetOrdinal("subscription_type"))
                ? null : reader.GetString("subscription_type");
            rawRenewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date"))
                ? null : reader.GetDateTime("subscription_renewal_date");
            credits = reader.IsDBNull(reader.GetOrdinal("credits")) ? 0 : reader.GetInt32("credits");
        }

        // Advance an expired renewal date by 28-day cycles until it is in the future.
        // Each passed cycle resets credits to the subscription plan maximum.
        var today = DateTime.UtcNow.Date;
        var renewalDate = rawRenewalDate;
        int cyclesPassed = 0;

        if (rawRenewalDate.HasValue && subscriptionType != null)
        {
            var d = rawRenewalDate.Value.Date;
            while (d < today)
            {
                d = d.AddDays(28);
                cyclesPassed++;
            }
            renewalDate = d;
        }

        if (cyclesPassed > 0)
        {
            credits = subscriptionType switch
            {
                "Rookie"       => 9,
                "Intermediate" => 13,
                "Advanced"     => 999,
                _              => credits
            };

            const string updateSql = """
                UPDATE users
                SET subscription_renewal_date = @newDate,
                    credits = @credits
                WHERE id = @userId
                """;
            await using var updateCmd = new MySqlCommand(updateSql, connection);
            updateCmd.Parameters.AddWithValue("@newDate", renewalDate!.Value.Date);
            updateCmd.Parameters.AddWithValue("@credits", credits);
            updateCmd.Parameters.AddWithValue("@userId", userId);
            await updateCmd.ExecuteNonQueryAsync();
        }

        return Results.Ok(new SubscriptionStatusDto
        {
            CurrentSubscriptionType = subscriptionType,
            RenewalDate             = renewalDate?.ToString("yyyy-MM-dd"),
            PendingSubscriptionChange = null,
            PendingBillingCycle       = null,
            Credits                   = credits
        });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error getting subscription status: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het ophalen van de abonnementsstatus.");
    }
})
.WithName("GetSubscriptionStatus")
.WithTags("Abonnementen")
.WithSummary("Haal de abonnementsstatus van een gebruiker op (type, verlengdatum, credits)")
.Produces<SubscriptionStatusDto>(200)
.Produces(404);

// POST /subscriptions/cancel — Sets subscription_type to NULL and credits to 0,
// effectively cancelling the subscription immediately.
app.MapPost("/subscriptions/cancel", async (IConfiguration configuration, SubscriptionCancelRequestDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    if (request.UserId <= 0)
    {
        return Results.Ok(new SubscriptionChangeResponseDto
        {
            Success = false,
            Message = "Gebruiker ID is verplicht."
        });
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Get current subscription info
        const string selectSql = """
            SELECT subscription_type, subscription_renewal_date
            FROM users
            WHERE id = @userId
            LIMIT 1;
            """;

        await using var selectCommand = new MySqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@userId", request.UserId);

        await using var reader = await selectCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Gebruiker niet gevonden."
            });
        }

        var currentSubscription = reader.IsDBNull(reader.GetOrdinal("subscription_type"))
            ? null
            : reader.GetString("subscription_type");

        if (string.IsNullOrEmpty(currentSubscription))
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Je hebt geen actief abonnement."
            });
        }

        var renewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date"))
            ? DateTime.Now.AddMonths(1)
            : reader.GetDateTime("subscription_renewal_date");

        await reader.CloseAsync();

        const string updateSql = """
            UPDATE users
            SET subscription_type = NULL,
                credits = 0
            WHERE id = @userId;
            """;

        await using var updateCommand = new MySqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@userId", request.UserId);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = true,
                Message = $"Je abonnement is stopgezet op {renewalDate:dd-MM-yyyy}.",
                EffectiveDate = renewalDate.ToString("yyyy-MM-dd"),
                NewSubscriptionType = null,
                BillingCycle = "cancelled"
            });
        }
        else
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Abonnement kon niet worden geannuleerd."
            });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error cancelling subscription: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het annuleren van het abonnement.");
    }
})
.WithName("CancelSubscription")
.WithTags("Abonnementen")
.WithSummary("Stop het abonnement van een gebruiker (zet type op NULL en credits op 0)")
.Produces<SubscriptionChangeResponseDto>(200);

// POST /subscriptions/change-billing — Updates the subscription renewal date to 1 month
// or 1 year from now depending on the requested billing cycle.
app.MapPost("/subscriptions/change-billing", async (IConfiguration configuration, ChangeBillingCycleRequestDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

    if (request.UserId <= 0)
    {
        return Results.Ok(new SubscriptionChangeResponseDto
        {
            Success = false,
            Message = "Gebruiker ID is verplicht."
        });
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Get current subscription info
        const string selectSql = """
            SELECT subscription_type, subscription_renewal_date
            FROM users
            WHERE id = @userId
            LIMIT 1;
            """;

        await using var selectCommand = new MySqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@userId", request.UserId);

        await using var reader = await selectCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Gebruiker niet gevonden."
            });
        }

        var currentSubscription = reader.IsDBNull(reader.GetOrdinal("subscription_type"))
            ? null
            : reader.GetString("subscription_type");

        if (string.IsNullOrEmpty(currentSubscription))
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Je hebt geen actief abonnement."
            });
        }

        var renewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date"))
            ? DateTime.Now.AddMonths(1)
            : reader.GetDateTime("subscription_renewal_date");

        await reader.CloseAsync();

        // Extend renewal date by 1 month or 1 year depending on chosen cycle
        var newRenewalDate = request.IsYearly
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);

        const string updateSql = """
            UPDATE users
            SET subscription_renewal_date = @renewalDate
            WHERE id = @userId;
            """;

        await using var updateCommand = new MySqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@renewalDate", newRenewalDate.Date);
        updateCommand.Parameters.AddWithValue("@userId", request.UserId);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            var billingCycleText = request.IsYearly ? "jaarlijks" : "maandelijks";
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = true,
                Message = $"Je factureringsperiode is gewijzigd naar {billingCycleText}.",
                EffectiveDate = newRenewalDate.ToString("yyyy-MM-dd"),
                NewSubscriptionType = currentSubscription,
                BillingCycle = request.IsYearly ? "yearly" : "monthly"
            });
        }
        else
        {
            return Results.Ok(new SubscriptionChangeResponseDto
            {
                Success = false,
                Message = "Factureringsperiode kon niet worden gewijzigd."
            });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error changing billing cycle: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het wijzigen van de factureringsperiode.");
    }
})
.WithName("ChangeBillingCycle")
.WithTags("Abonnementen")
.WithSummary("Wijzig de factureringsperiode van een gebruiker (maandelijks/jaarlijks)")
.Produces<SubscriptionChangeResponseDto>(200);

// DELETE /upload/photo/{userId} — Removes the user's profile photo file and clears photo_url in the DB.
app.MapDelete("/upload/photo/{userId:int}", async (int userId, IConfiguration configuration) =>
{
    try
    {
        foreach (var old in Directory.GetFiles(uploadsDir, $"user_{userId}_*"))
            File.Delete(old);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(
            "UPDATE users SET photo_url = NULL WHERE id = @id", connection);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync();

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[API] Photo delete error for user {userId}: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het verwijderen van de foto.");
    }
})
.WithName("DeleteProfilePhoto")
.WithTags("Profiel")
.WithSummary("Verwijder de profielfoto van een gebruiker (bestand + database)")
.Produces<object>(200);

// POST /upload/photo/{userId} — Accepts a multipart/form-data upload (max 5 MB, jpg/png/webp),
// saves it to the /uploads directory, and updates the user's photo_url in the database.
// Returns { "photoUrl": "http://..." } so the client can display the new image immediately.
app.MapPost("/upload/photo/{userId:int}", async (int userId, HttpRequest request, IConfiguration configuration) =>
{
    if (!request.HasFormContentType || request.Form.Files.Count == 0)
        return Results.BadRequest("Geen bestand ontvangen.");

    var photo = request.Form.Files[0];
    if (photo.Length == 0)
        return Results.BadRequest("Bestand is leeg.");

    var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
    if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        return Results.BadRequest("Alleen JPG, PNG en WebP zijn toegestaan.");

    if (photo.Length > 5 * 1024 * 1024)
        return Results.BadRequest("Bestand te groot (max 5 MB).");

    // Remove previous photo for this user
    foreach (var old in Directory.GetFiles(uploadsDir, $"user_{userId}_*"))
        File.Delete(old);

    var fileName  = $"user_{userId}_{Guid.NewGuid():N}{ext}";
    var filePath  = Path.Combine(uploadsDir, fileName);
    await using (var stream = File.Create(filePath))
        await photo.CopyToAsync(stream);

    var photoUrl = $"{request.Scheme}://{request.Host}/uploads/{fileName}";

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    await using var cmd = new MySqlCommand(
        "UPDATE users SET photo_url = @url WHERE id = @id", connection);
    cmd.Parameters.AddWithValue("@url", photoUrl);
    cmd.Parameters.AddWithValue("@id", userId);
    var rows = await cmd.ExecuteNonQueryAsync();

    return rows > 0
        ? Results.Ok(new { photoUrl })
        : Results.NotFound("Gebruiker niet gevonden.");
})
.WithName("UploadProfilePhoto")
.WithTags("Profiel")
.WithSummary("Upload een profielfoto (multipart/form-data, jpg/png/webp, max 5 MB)")
.Produces<object>(200)
.Produces(404)
.DisableAntiforgery();

// GET /users/{userId}/notifications — Returns up to 100 notifications for the user,
// newest first. Called by NotificationService.LoadAsync() after login.
app.MapGet("/users/{userId:int}/notifications", async (int userId, IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string niet gevonden.");

    var items = new List<NotificationDto>();
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = """
        SELECT id, title, message, type, is_read, created_at
        FROM notifications
        WHERE user_id = @userId
        ORDER BY created_at DESC
        LIMIT 100
        """;
    await using var cmd = new MySqlCommand(sql, connection);
    cmd.Parameters.AddWithValue("@userId", userId);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        items.Add(new NotificationDto
        {
            Id        = reader.GetString("id"),
            Title     = reader.GetString("title"),
            Message   = reader.GetString("message"),
            Type      = reader.GetInt32("type"),
            IsRead    = reader.GetBoolean("is_read"),
            CreatedAt = reader.GetDateTime("created_at")
        });
    }
    return Results.Ok(items);
})
.WithName("GetNotifications")
.WithTags("Notificaties")
.WithSummary("Haal notificaties van een gebruiker op (max 100, nieuwste eerst)")
.Produces<IEnumerable<NotificationDto>>(200);

// POST /users/{userId}/notifications — Inserts a notification into the database.
// ON DUPLICATE KEY UPDATE is a no-op, making this safe to retry (idempotent).
app.MapPost("/users/{userId:int}/notifications", async (int userId, IConfiguration configuration, NotificationDto dto) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string niet gevonden.");

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO notifications (id, user_id, title, message, type, is_read, created_at)
            VALUES (@id, @userId, @title, @message, @type, 0, @createdAt)
            ON DUPLICATE KEY UPDATE id = id
            """;
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", dto.Id);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@title", dto.Title);
        cmd.Parameters.AddWithValue("@message", dto.Message);
        cmd.Parameters.AddWithValue("@type", dto.Type);
        cmd.Parameters.AddWithValue("@createdAt", dto.CreatedAt);
        await cmd.ExecuteNonQueryAsync();

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error saving notification: {ex.Message}");
        return Results.Problem("Fout bij opslaan notificatie.");
    }
})
.WithName("CreateNotification")
.WithTags("Notificaties")
.WithSummary("Sla een notificatie op voor een gebruiker (idempotent via ON DUPLICATE KEY)")
.Produces<object>(200);

// PUT /users/{userId}/notifications/mark-all-read — Sets is_read=1 for all of
// the user's notifications. Called when the user opens the notifications page.
app.MapPut("/users/{userId:int}/notifications/mark-all-read", async (int userId, IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string niet gevonden.");

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    const string sql = "UPDATE notifications SET is_read = 1 WHERE user_id = @userId";
    await using var cmd = new MySqlCommand(sql, connection);
    cmd.Parameters.AddWithValue("@userId", userId);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { success = true });
})
.WithName("MarkAllNotificationsRead")
.WithTags("Notificaties")
.WithSummary("Markeer alle notificaties van een gebruiker als gelezen")
.Produces<object>(200);

// GET /lessons/{lessonId}/bikes?userId={id} — Returns 16 bikes (4×4) with availability.
// isAvailable = no one has booked that seat; isSelectedByCurrentUser = current user's own seat.
app.MapGet("/lessons/{lessonId:int}/bikes", async (int lessonId, IConfiguration configuration, int? userId) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql = "SELECT row_number, bike_number, user_id FROM bike_reservations WHERE lesson_id = @lessonId";
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@lessonId", lessonId);
    await using var reader = await command.ExecuteReaderAsync();

    var taken = new List<(int row, int bike, int uid)>();
    while (await reader.ReadAsync())
        taken.Add((reader.GetInt32("row_number"), reader.GetInt32("bike_number"), reader.GetInt32("user_id")));

    var bikes = new List<object>();
    for (int row = 1; row <= 4; row++)
        for (int bike = 1; bike <= 4; bike++)
        {
            int idx = taken.FindIndex(t => t.row == row && t.bike == bike);
            bool isTaken = idx >= 0;
            bikes.Add(new
            {
                rowNumber               = row,
                bikeNumber              = bike,
                isAvailable             = !isTaken,
                isSelectedByCurrentUser = isTaken && userId.HasValue && taken[idx].uid == userId.Value
            });
        }

    return Results.Ok(bikes);
})
.WithName("GetLessonBikes")
.WithTags("Reserveringen")
.WithSummary("Haal fietsreserveringen op voor een spinningles (16 fietsen, 4 rijen × 4)")
.Produces<object>(200);

// POST /lessons/{lessonId}/bikes — Reserves or switches to a different bike for the user.
// Allows changing bike (UPDATE) if the user already has one; blocks if target seat is taken by another user.
app.MapPost("/lessons/{lessonId:int}/bikes", async (int lessonId, IConfiguration configuration, BikeReservationRequestDto request) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    if (request.RowNumber < 1 || request.RowNumber > 4 || request.BikeNumber < 1 || request.BikeNumber > 4)
        return Results.Ok(new { success = false, message = "Ongeldige fietspositie." });

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Does this user already have a bike for this lesson?
        const string checkUserSql = "SELECT id FROM bike_reservations WHERE lesson_id = @lessonId AND user_id = @userId";
        await using var checkUserCmd = new MySqlCommand(checkUserSql, connection);
        checkUserCmd.Parameters.AddWithValue("@lessonId", lessonId);
        checkUserCmd.Parameters.AddWithValue("@userId",   request.UserId);
        var existingId = await checkUserCmd.ExecuteScalarAsync();

        if (existingId != null)
        {
            // Changing bike — make sure the target isn't already claimed by someone else
            const string checkBikeSql = "SELECT user_id FROM bike_reservations WHERE lesson_id = @lessonId AND row_number = @row AND bike_number = @bike";
            await using var checkBikeCmd = new MySqlCommand(checkBikeSql, connection);
            checkBikeCmd.Parameters.AddWithValue("@lessonId", lessonId);
            checkBikeCmd.Parameters.AddWithValue("@row",      request.RowNumber);
            checkBikeCmd.Parameters.AddWithValue("@bike",     request.BikeNumber);
            var takenByObj = await checkBikeCmd.ExecuteScalarAsync();
            if (takenByObj != null && Convert.ToInt32(takenByObj) != request.UserId)
                return Results.Ok(new { success = false, message = "Deze fiets is al bezet door een andere gebruiker." });

            const string updateSql = "UPDATE bike_reservations SET row_number = @row, bike_number = @bike WHERE lesson_id = @lessonId AND user_id = @userId";
            await using var updateCmd = new MySqlCommand(updateSql, connection);
            updateCmd.Parameters.AddWithValue("@row",      request.RowNumber);
            updateCmd.Parameters.AddWithValue("@bike",     request.BikeNumber);
            updateCmd.Parameters.AddWithValue("@lessonId", lessonId);
            updateCmd.Parameters.AddWithValue("@userId",   request.UserId);
            await updateCmd.ExecuteNonQueryAsync();
            return Results.Ok(new { success = true, rowNumber = request.RowNumber, bikeNumber = request.BikeNumber, message = "Fiets bijgewerkt." });
        }

        // New bike reservation
        const string insertSql = """
            INSERT INTO bike_reservations (lesson_id, user_id, row_number, bike_number, created_at)
            VALUES (@lessonId, @userId, @row, @bike, NOW())
            """;
        await using var insertCmd = new MySqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@lessonId", lessonId);
        insertCmd.Parameters.AddWithValue("@userId",   request.UserId);
        insertCmd.Parameters.AddWithValue("@row",      request.RowNumber);
        insertCmd.Parameters.AddWithValue("@bike",     request.BikeNumber);
        try
        {
            await insertCmd.ExecuteNonQueryAsync();
            return Results.Ok(new { success = true, rowNumber = request.RowNumber, bikeNumber = request.BikeNumber, message = "Fiets succesvol gereserveerd." });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Results.Ok(new { success = false, message = "Deze fiets is al bezet." });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error reserving bike: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het reserveren van de fiets.");
    }
})
.WithName("ReserveBike")
.WithTags("Reserveringen")
.WithSummary("Reserveer of wissel een fiets voor een spinningles")
.Produces<object>(200);

// DELETE /lessons/{lessonId}/bikes?userId={id} — Releases the user's bike reservation.
app.MapDelete("/lessons/{lessonId:int}/bikes", async (int lessonId, IConfiguration configuration, int userId) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = "DELETE FROM bike_reservations WHERE lesson_id = @lessonId AND user_id = @userId";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@lessonId", lessonId);
        command.Parameters.AddWithValue("@userId",   userId);
        await command.ExecuteNonQueryAsync();

        return Results.Ok(new { success = true, message = "Fietsreservering vrijgegeven." });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error releasing bike: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het vrijgeven van de fiets.");
    }
})
.WithName("ReleaseBike")
.WithTags("Reserveringen")
.WithSummary("Geef een fietsreservering vrij voor een spinningles")
.Produces<object>(200);

app.Run();

// ── DTOs ─────────────────────────────────────────────────────────────────────
// Data Transfer Objects used by the Minimal API endpoints.
// JsonPropertyName attributes control the camelCase JSON keys returned to clients.

// Request body for POST /auth/login
public class LoginRequestDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("credits")]
    public int? Credits { get; set; }

    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    [JsonPropertyName("subscriptionRenewalDate")]
    public string? SubscriptionRenewalDate { get; set; }
}

public class ReservationRequestDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
}

// Subscription DTOs
public class SubscriptionPlanDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("monthlyPrice")]
    public decimal MonthlyPrice { get; set; }

    [JsonPropertyName("yearlyPrice")]
    public decimal YearlyPrice { get; set; }

    [JsonPropertyName("credits")]
    public int Credits { get; set; }

    [JsonPropertyName("isUnlimited")]
    public bool IsUnlimited { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class SubscriptionChangeRequestDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("newSubscriptionType")]
    public string NewSubscriptionType { get; set; } = string.Empty;

    [JsonPropertyName("isYearly")]
    public bool IsYearly { get; set; }
}

public class SubscriptionChangeResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("effectiveDate")]
    public string? EffectiveDate { get; set; }

    [JsonPropertyName("newSubscriptionType")]
    public string? NewSubscriptionType { get; set; }

    [JsonPropertyName("billingCycle")]
    public string? BillingCycle { get; set; }
}

public class SubscriptionStatusDto
{
    [JsonPropertyName("currentSubscriptionType")]
    public string? CurrentSubscriptionType { get; set; }

    [JsonPropertyName("renewalDate")]
    public string? RenewalDate { get; set; }

    [JsonPropertyName("pendingSubscriptionChange")]
    public string? PendingSubscriptionChange { get; set; }

    [JsonPropertyName("pendingBillingCycle")]
    public string? PendingBillingCycle { get; set; }

    [JsonPropertyName("credits")]
    public int Credits { get; set; }
}

public class SubscriptionCancelRequestDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
}

public class ChangeBillingCycleRequestDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("isYearly")]
    public bool IsYearly { get; set; }
}

// Strongly-typed DTO for the three dropdown endpoints (workouts / locations / instructors).
// Must be a named class — anonymous types are serialised as empty objects {} by System.Text.Json.
// Color is only populated by /workouts; null for /locations and /instructors.
public class DropdownItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

public class UserLessonDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("workoutName")]
    public string WorkoutName { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("instructorName")]
    public string InstructorName { get; set; } = string.Empty;

    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = string.Empty;
}

public class LessonSaveDto
{
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("maxParticipants")]
    public int MaxParticipants { get; set; }

    [JsonPropertyName("workoutId")]
    public int WorkoutId { get; set; }

    [JsonPropertyName("instructorId")]
    public int InstructorId { get; set; }

    [JsonPropertyName("locationId")]
    public int LocationId { get; set; }
}

public class AddMemberDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
}

// Request body for POST /lessons/{lessonId}/bikes
public class BikeReservationRequestDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("rowNumber")]
    public int RowNumber { get; set; }

    [JsonPropertyName("bikeNumber")]
    public int BikeNumber { get; set; }
}

// DTO for the notification endpoints — used for both read (GET) and write (POST) operations.
public class NotificationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
