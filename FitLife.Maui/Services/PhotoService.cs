using System.Net.Http.Json;

namespace FitLife.Maui.Services;

public class PhotoService : IPhotoService
{
    private readonly HttpClient _http;

    public PhotoService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> PickAndUploadAsync(int userId)
    {
        // Let the user choose between gallery and camera
        var page = Application.Current?.Windows[0].Page
                   ?? Application.Current?.MainPage;
        if (page is null) return null;

        var action = await page.DisplayActionSheet(
            "Profielfoto kiezen",
            "Annuleren",
            null,
            "Galerij / Bestanden",
            "Camera");

        FileResult? result = action switch
        {
            "Camera"             => await TryCapturePhotoAsync(),
            "Galerij / Bestanden" => await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                                     {
                                         Title = "Kies een profielfoto"
                                     }),
            _ => null
        };

        if (result is null) return null;

        return await UploadResultAsync(userId, result);
    }

    private static async Task<FileResult?> TryCapturePhotoAsync()
    {
        if (!MediaPicker.IsCaptureSupported)
        {
            var page = Application.Current?.Windows[0].Page
                       ?? Application.Current?.MainPage;
            if (page is not null)
                await page.DisplayAlert("Camera", "Camera is niet beschikbaar op dit apparaat.", "OK");
            return null;
        }
        return await MediaPicker.CapturePhotoAsync();
    }

    private async Task<string?> UploadResultAsync(int userId, FileResult result)
    {
        await using var stream = await result.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);

        var ext = Path.GetExtension(result.FileName).ToLowerInvariant();
        var mime = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".webp"           => "image/webp",
            _                 => "image/jpeg"
        };
        fileContent.Headers.ContentType = new(mime);
        content.Add(fileContent, "photo", result.FileName);

        var response = await _http.PostAsync($"upload/photo/{userId}", content);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadFromJsonAsync<PhotoUploadResult>();
        return json?.PhotoUrl;
    }

    private record PhotoUploadResult(string PhotoUrl);
}