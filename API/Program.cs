using API.Infrastructure.Database;
using API.Repositories.Implementations;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Implementations;
using API.Services.Interfaces;
using API.src.Repositories.Implementations;
using API.Storage;
using API.Storage.Implementations;
using API.Storage.Interfaces;
using DotNetEnv;
using MailKit;
using Microsoft.EntityFrameworkCore;

Env.Load();
// App setup: create builder + dependency container
var builder = WebApplication.CreateBuilder(args);

// MVC: enable controllers and routing
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// API docs: enable Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI: register application services
/*
 * Transient: A new instance is provided every time the service is requested.
 * Scoped: A single instance is provided per request.
 * Singleton: A single instance is created and shared throughout the application's lifetime.
 */

//Users
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Workouts
builder.Services.AddScoped<IWorkoutRepository, WorkoutRepository>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();

// Instructors
builder.Services.AddScoped<IInstructorRepository, InstructorRepository>();
builder.Services.AddScoped<IInstructorService, InstructorService>();

// Lessons
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<ILessonService, LessonService>();

// Members
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IMemberService, MemberService>();

// Reservations
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IReservationService, ReservationService>();

// Subscriptions
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Waitlist
builder.Services.AddScoped<IWaitlistService, WaitlistService>();

// Photo Storage
builder.Services.AddScoped<IPhotoStorage, LocalPhotoStorage>();

// Monitoring: health check endpoint
builder.Services.AddHealthChecks();

// ORM: configure EF Core with MySQL
builder.Services.AddDbContextPool<ApiDbContext>(options =>
{
    // Try to get connection string from environment/Docker
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "my_local_db";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "secret";

        // Fallback for local debugging
        Console.WriteLine("DefaultConnection not found in environment. Using local MySQL connection.");
        connectionString = $"Server=localhost;Port=3306;Database={database};User={user};Password={password};";
    }
    else
    {
        Console.WriteLine("Using DefaultConnection from environment.");
    }

    // Wait for MySQL if needed (optional for local debugging, you can skip retries locally)
    ServerVersion? serverVersion = null;
    var retries = 0;
    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);

    while (serverVersion == null && retries < maxRetries)
    {
        try
        {
            serverVersion = ServerVersion.AutoDetect(connectionString);
        }
        catch (MySqlConnector.MySqlException)
        {
            retries++;
            Console.WriteLine($"MySQL not ready yet. Retry {retries}/{maxRetries} in {delay.TotalSeconds} seconds...");
            Thread.Sleep(delay);
        }
    }

    if (serverVersion == null)
        throw new InvalidOperationException("Could not connect to MySQL to detect server version.");

    options.UseMySql(
        connectionString,
        serverVersion,
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        }
    );
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWasm", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5031" // wa local
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// App build: finalize configuration
var app = builder.Build();

// Network: configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security: redirect HTTP to HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // serves wwwroot/*

// Routing: enable routing
app.UseRouting();

// Security: CORS
app.UseCors("BlazorWasm");

// Security: authorization middleware
app.UseAuthorization();

// Monitoring: expose /health
app.MapHealthChecks("/health");

// Routing: map controller endpoints
app.MapControllers();

// Database: apply pending migrations at startup and seed some mock data
using (var scope = app.Services.CreateScope())
{
    // Ensure the database and tables are there. This is not production-ready, but it simplifies development and testing.
    // Since this is a school project which always destroys the database on recreation it does not matter
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApiDbContext>();
    db.Database.EnsureCreated();

    // Seed data
    try
    {
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Seeding produced an error: " + ex.Message);
    }
}

// Runtime: start web application
app.Run();

// # Test