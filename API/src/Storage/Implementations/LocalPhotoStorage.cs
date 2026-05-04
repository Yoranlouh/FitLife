using System.Diagnostics;
using API.Storage.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using SharedLibrary.Domain.Entities;

namespace API.Storage.Implementations;

public class LocalPhotoStorage : IPhotoStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _http;

    public LocalPhotoStorage(IWebHostEnvironment env, IHttpContextAccessor http)
    {
        _env = env;
        _http = http;
    }

    public async Task<PhotoSaveResult> SaveAsync(Stream stream, string contentType, string originalFileName,
        string folder, CancellationToken ct, int creatorId)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var id = Guid.NewGuid().ToString("n");
        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "default" : SanitizeFolder(folder);
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads",
            safeFolder, creatorId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{id}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await stream.CopyToAsync(fileStream, ct);
        }

        // Build a public URL
        var req = _http.HttpContext?.Request;
        if (req is null) throw new InvalidOperationException("No HTTP context for URL building.");

        var baseUrl = $"{req.Scheme}://{req.Host}";
        var urlPath = $"/uploads/{Uri.EscapeDataString(safeFolder)}/{Uri.EscapeDataString(creatorId.ToString())}/{Uri.EscapeDataString(fileName)}";
        var url = baseUrl + urlPath;

        var size = new FileInfo(fullPath).Length;
        var storageKey = Path.Combine("uploads", safeFolder, fileName).Replace("\\", "/");

        return new PhotoSaveResult(id, url, storageKey, size, contentType, creatorId);
    }

    public async Task<Photo> GetByCreatorAndType(int EntityId, string type)
    {
        // Determine sanitized folder name (same rules as SaveAsync)
        var safeType = string.IsNullOrWhiteSpace(type) ? "default" : SanitizeFolder(type);

        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads",
            safeType, EntityId.ToString());
        Debug.WriteLine(uploadsRoot);
        if (!Directory.Exists(uploadsRoot))
            return null!; // no photo for this creator/type

        // Find files and pick the most recently modified one
        var files = Directory.GetFiles(uploadsRoot);
        if (files.Length == 0)
            return null!;

        var latest = files
            .Select(p => new FileInfo(p))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .First();

        var fileName = Path.GetFileName(latest.FullName);

        // Build public URL
        var req = _http.HttpContext?.Request;
        if (req is null) throw new InvalidOperationException("No HTTP context for URL building.");

        var baseUrl = $"{req.Scheme}://{req.Host}";
        var urlPath = $"/uploads/{Uri.EscapeDataString(safeType)}/{Uri.EscapeDataString(EntityId.ToString())}/{Uri.EscapeDataString(fileName)}";
        var url = baseUrl + urlPath;

        // Try to infer content type from extension
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
            contentType = "application/octet-stream";

        var storageKey = Path.Combine("uploads", safeType, fileName).Replace("\\", "/");

        var photo = new Photo
        {
            Id = Path.GetFileNameWithoutExtension(fileName),
            Url = url,
            StorageKey = storageKey,
            Size = latest.Length,
            ContentType = contentType,
            EntityId = EntityId
        };

        return await Task.FromResult(photo);

    }

    private static string SanitizeFolder(string folder)
    {
        // keep it simple: letters, numbers, dash, underscore
        var cleaned = new string(folder.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "default" : cleaned;
    }
}