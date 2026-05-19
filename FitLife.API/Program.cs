using MySqlConnector;
using SharedLibrary.DTOs.Responses;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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
            InstructorId = reader.IsDBNull(reader.GetOrdinal("instructor_id")) ? 0 : reader.GetInt32("instructor_id"),
            InstructorName = reader.IsDBNull(reader.GetOrdinal("instructor_name")) ? "Onbekende instructeur" : reader.GetString("instructor_name"),
            LocationId = reader.GetInt32("location_id"),
            LocationName = reader.GetString("location_name"),
            CurrentParticipantCount = currentCount,
            WaitlistCount = waitlistCount
        });
    }

    return Results.Ok(lessons);
});

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

app.MapGet("/lessons/{lessonId:int}/waitlist", async (int lessonId, IConfiguration configuration) =>
{
    // Waitlist functionality not implemented yet - return empty list
    // TODO: Create waitlist_entries table and implement waitlist logic
    var waitlist = new List<ParticipantResponse>();
    return Results.Ok(waitlist);
});

// Authentication endpoint - POST /auth/login
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
            SELECT id, email, password_hash, display_name, role
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
            var role = reader.GetString("role");

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
                    Email = email,
                    Role = role
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

// Helper method to compute SHA256 hash (for demo purposes)
// In production, use proper password hashing like BCrypt
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

app.Run();

// DTO's for authentication - must be declared after app.Run() for top-level statements
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

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}
