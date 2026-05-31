namespace FitLife.Maui.Services;

public interface IPhotoService
{
    Task<string?> PickAndUploadAsync(int userId);
}