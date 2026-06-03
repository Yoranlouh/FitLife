using MySqlConnector;
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

// Register OpenAPI (Swagger) metadata — accessible at /openapi in development
builder.Services.AddOpenApi();

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

    const string sql = "SELECT id, name FROM workouts ORDER BY name";
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        items.Add(new DropdownItemDto { Id = reader.GetInt32("id"), Name = reader.GetString("name") });

    return Results.Ok(items);
});

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
});

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
});

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
            (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
            (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
        FROM lessons l
        INNER JOIN workouts w ON w.id = l.workout_id
        LEFT JOIN users u ON u.id = l.instructor_id
        INNER JOIN locations loc ON loc.id = l.location_id
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
});

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
});

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
});

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
});

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

        // Insert reservation without credit deduction
        const string insertSql = """
            INSERT INTO reservations (lesson_id, member_id, reservation_date, is_cancelled)
            VALUES (@lessonId, @userId, NOW(), 0)
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
});

// GET /lessons — Returns the full lesson catalogue with randomised participant counts
// to make the schedule look realistic during development/demo.
// In production the random counts should be replaced with real DB data.
app.MapGet("/lessons", async (IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'DefaultConnection' is niet gevonden.");
    }

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
            (SELECT COUNT(*) FROM reservations r WHERE r.lesson_id = l.id AND r.is_cancelled = 0) AS current_participants,
            (SELECT COUNT(*) FROM waitlist_entries wle WHERE wle.lesson_id = l.id) AS waitlist_count
        FROM lessons l
        INNER JOIN workouts w ON w.id = l.workout_id
        LEFT JOIN users u ON u.id = l.instructor_id
        INNER JOIN locations loc ON loc.id = l.location_id
        ORDER BY l.start_time;
        """;

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    var random = new Random();

    while (await reader.ReadAsync())
    {
        var lessonId = reader.GetInt32("id");
        var maxParticipants = reader.GetInt32("max_participants");
        
        // Gebruik echte data uit DB OF genereer willekeurige data zoals gevraagd
        // We overschrijven de database counts met willekeurige waarden voor de 'look'
        int currentCount;
        int waitlistCount = 0;

        int rnd = random.Next(100);
        if (rnd < 20) // 20% kans op vol + wachtlijst
        {
            currentCount = maxParticipants;
            waitlistCount = random.Next(1, 6);
        }
        else if (rnd < 50) // 30% kans op exact vol
        {
            currentCount = maxParticipants;
        }
        else if (rnd < 80) // 30% kans op ongeveer de helft
        {
            currentCount = maxParticipants / 2 + random.Next(-2, 3);
            if (currentCount < 0) currentCount = 0;
            if (currentCount > maxParticipants) currentCount = maxParticipants;
        }
        else // 20% kans op (vrijwel) leeg
        {
            currentCount = random.Next(0, 3);
            if (currentCount > maxParticipants) currentCount = maxParticipants;
        }

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
            IsBooked = random.Next(100) < 15 // 15% kans dat de gebruiker is aangemeld
        });
    }

    return Results.Ok(lessons);
});

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
});

// GET /lessons/{lessonId}/waitlist — Waitlist query placeholder.
// Returns an empty list until the waitlist_entries table is fully implemented.
app.MapGet("/lessons/{lessonId:int}/waitlist", async (int lessonId, IConfiguration configuration) =>
{
    var waitlist = new List<ParticipantResponse>();
    return Results.Ok(waitlist);
});

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
});

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
                return Results.Ok(new { success = false, message = "Les is vol. Je kunt je aanmelden voor de wachtlijst." });
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

            // Create reservation
            const string insertReservationSql = """
                INSERT INTO reservations (lesson_id, member_id, reservation_date, is_cancelled)
                VALUES (@lessonId, @userId, NOW(), 0)
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
});

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
            // Check if reservation exists
            const string checkSql = "SELECT id FROM reservations WHERE lesson_id = @lessonId AND member_id = @userId AND is_cancelled = 0";
            await using var checkCmd = new MySqlCommand(checkSql, connection, transaction);
            checkCmd.Parameters.AddWithValue("@lessonId", lessonId);
            checkCmd.Parameters.AddWithValue("@userId", userId);
            
            var reservationId = await checkCmd.ExecuteScalarAsync();
            if (reservationId == null)
            {
                await transaction.RollbackAsync();
                return Results.Ok(new { success = false, message = "Geen actieve reservering gevonden voor deze les." });
            }

            // Cancel reservation
            const string cancelSql = "UPDATE reservations SET is_cancelled = 1 WHERE id = @reservationId";
            await using var cancelCmd = new MySqlCommand(cancelSql, connection, transaction);
            cancelCmd.Parameters.AddWithValue("@reservationId", reservationId);
            await cancelCmd.ExecuteNonQueryAsync();

            // Refund 1 credit
            const string refundSql = "UPDATE users SET credits = credits + 1 WHERE id = @userId";
            await using var refundCmd = new MySqlCommand(refundSql, connection, transaction);
            refundCmd.Parameters.AddWithValue("@userId", userId);
            await refundCmd.ExecuteNonQueryAsync();

            // Get updated credits
            const string getCreditsSql = "SELECT credits FROM users WHERE id = @userId";
            await using var getCreditsCmd = new MySqlCommand(getCreditsSql, connection, transaction);
            getCreditsCmd.Parameters.AddWithValue("@userId", userId);
            var updatedCredits = await getCreditsCmd.ExecuteScalarAsync();

            await transaction.CommitAsync();

            return Results.Ok(new { success = true, message = "Reservering geannuleerd! 1 credit is teruggegeven.", remainingCredits = Convert.ToInt32(updatedCredits) });
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
});

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
});

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
});

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
});

// GET /subscriptions/status/{userId} — Returns the user's current subscription type,
// renewal date, and credit balance. Used by ProfileViewModel and SubscriptionViewModel.
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

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var status = new SubscriptionStatusDto
            {
                CurrentSubscriptionType = reader.IsDBNull(reader.GetOrdinal("subscription_type"))
                    ? null
                    : reader.GetString("subscription_type"),
                RenewalDate = reader.IsDBNull(reader.GetOrdinal("subscription_renewal_date"))
                    ? null
                    : reader.GetDateTime("subscription_renewal_date").ToString("yyyy-MM-dd"),
                PendingSubscriptionChange = null,
                PendingBillingCycle = null,
                Credits = reader.IsDBNull(reader.GetOrdinal("credits")) ? 0 : reader.GetInt32("credits")
            };

            return Results.Ok(status);
        }
        else
        {
            return Results.NotFound();
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error getting subscription status: {ex.Message}");
        return Results.Problem("Er is een fout opgetreden bij het ophalen van de abonnementsstatus.");
    }
});

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
});

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
});

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
}).DisableAntiforgery();

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
});

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
});

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
});

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
public class DropdownItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
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
