using Microsoft.AspNetCore.Components.Forms;

namespace FitLife.BlazorWebApp.Services;

public interface IPhotoUploadService
{
    Task<(bool Success, string? PhotoUrl, string Error)> UploadAsync(int userId, IBrowserFile file);
}