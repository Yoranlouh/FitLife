using MySqlConnector;
using SharedLibrary.DTOs.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
            l.end_time,
            l.max_participants,
            l.workout_id,
            w.name AS workout_name,
            l.instructor_id,
            CONCAT(u.first_name, ' ', u.last_name) AS instructor_name,
            l.location_id,
            loc.name AS location_name
        FROM lessons l
        INNER JOIN workouts w ON w.id = l.workout_id
        INNER JOIN users u ON u.id = l.instructor_id
        INNER JOIN locations loc ON loc.id = l.location_id
        ORDER BY l.start_time;
        """;

    await using var command = new MySqlCommand(sql, connection);
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
            InstructorId = reader.GetInt32("instructor_id"),
            InstructorName = reader.GetString("instructor_name"),
            LocationId = reader.GetInt32("location_id"),
            LocationName = reader.GetString("location_name")
        });
    }

    return Results.Ok(lessons);
});

app.Run();
