using FitLife.BlazorWebApp.Components;
using FitLife.BlazorWebApp.Services;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using MySqlConnector;

// ──────────────────────────────────────────────────────────────────────────────
// FitLife Blazor Web App — startup configuration
// This is the ASP.NET Core Minimal Hosting entry point.
// Services are registered here (dependency injection) and the HTTP pipeline is built.
// ──────────────────────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// Enable Blazor Server with interactive rendering (SignalR-based)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Session middleware — keeps a lightweight session per HTTP connection
// Used as a fallback identifier when the auth cookie is absent
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout                  = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly              = true;
    options.Cookie.IsEssential           = true;
    options.Cookie.Name                  = ".FitLife.Session";
    options.Cookie.SecurePolicy          = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite              = SameSiteMode.Lax;
});

// Blazored.Toast — shows success/error popup messages in the admin UI
builder.Services.AddBlazoredToast();

// Handles saving user profile photos to wwwroot/uploads/
builder.Services.AddScoped<IPhotoUploadService, PhotoUploadService>();

// In-memory session store: maps session IDs (stored in the auth cookie) to user objects.
// Singleton so all Blazor circuits share the same store.
builder.Services.AddSingleton<ISessionService, SessionService>();

// Simulates a payment flow (in-memory so state survives across page navigations)
builder.Services.AddSingleton<IPaymentService, PaymentService>();

// Writes newly registered members to the database after payment is confirmed
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

// Database services — each scoped per HTTP request, all talk directly to MySQL
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IInstructorService, InstructorService>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Hosted background service: sets is_archived=1 on past lessons once per day
builder.Services.AddHostedService<LessonArchiveService>();

// Custom authentication scheme that validates the ".FitLife.Auth" cookie
builder.Services.AddAuthentication("CustomScheme")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, CustomAuthenticationHandler>("CustomScheme", options => { });

builder.Services.AddAuthorization();

// Provides the logged-in user's identity to [Authorize] attributes and AuthenticationState cascades
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Required so services can read the HTTP context (cookies, session) during Blazor Server rendering
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

// Serve wwwroot files including dynamically uploaded photos
app.UseStaticFiles();

// Ensure uploads directory exists on startup
var uploadsDir = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsDir);

// Auto-migrate: voeg color kolom toe aan workouts als die nog niet bestaat
try
{
    var connStr = app.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connStr))
    {
        await using var migConn = new MySqlConnection(connStr);
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

        // Auto-migrate: voeg is_archived kolom toe aan lessons als die nog niet bestaat
        const string checkArchived = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='lessons' AND COLUMN_NAME='is_archived'";
        await using var checkArchivedCmd = new MySqlCommand(checkArchived, migConn);
        if (Convert.ToInt32(await checkArchivedCmd.ExecuteScalarAsync()) == 0)
        {
            await using var addArchivedCmd = new MySqlCommand(
                "ALTER TABLE lessons ADD COLUMN is_archived TINYINT(1) NOT NULL DEFAULT 0;",
                migConn);
            await addArchivedCmd.ExecuteNonQueryAsync();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Migration] color kolom: {ex.Message}");
}

// Enable session middleware
app.UseSession();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Logout endpoint — runs in real HTTP context so it can delete the auth cookie
app.MapGet("/admin/logout", (HttpContext ctx, ISessionService sessionService) =>
{
    if (ctx.Request.Cookies.TryGetValue(".FitLife.Auth", out var sessionId))
        sessionService.RemoveUserSession(sessionId);

    ctx.Response.Cookies.Delete(".FitLife.Auth");
    return Results.Redirect("/admin/login");
});

app.Run();
