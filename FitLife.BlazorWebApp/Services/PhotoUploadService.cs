using Microsoft.AspNetCore.Components.Forms;
using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

public class PhotoUploadService : IPhotoUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration      _config;

    public PhotoUploadService(IWebHostEnvironment env, IConfiguration config)
    {
        _env    = env;
        _config = config;
    }

    public async Task<(bool Success, string? PhotoUrl, string Error)> UploadAsync(
        int userId, IBrowserFile file)
    {
        try
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            // Remove previous photo for this user
            foreach (var old in Directory.GetFiles(uploadsDir, $"user_{userId}_*"))
                File.Delete(old);

            var ext = Path.GetExtension(file.Name).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => ".jpg",
                ".png"            => ".png",
                ".webp"           => ".webp",
                _                 => ".jpg"
            };

            var fileName = $"user_{userId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Stream file to disk (max 5 MB)
            await using var readStream = file.OpenReadStream(5 * 1024 * 1024);
            await using var writeStream = File.Create(filePath);
            await readStream.CopyToAsync(writeStream);

            // Relative URL — works in any browser regardless of host/port
            var photoUrl = $"/uploads/{fileName}";

            // Persist to database
            var cs = _config.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("No connection string.");

            await using var connection = new MySqlConnection(cs);
            await connection.OpenAsync();
            await using var cmd = new MySqlCommand(
                "UPDATE users SET photo_url = @url WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("@url", photoUrl);
            cmd.Parameters.AddWithValue("@id", userId);
            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0
                ? (true, photoUrl, string.Empty)
                : (false, null, "Gebruiker niet gevonden in database.");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}