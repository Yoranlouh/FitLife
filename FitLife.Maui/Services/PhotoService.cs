using System.Net.Http.Json;

namespace FitLife.Maui.Services;

// Interface for picking a photo and uploading it as the user's profile picture.
public interface IPhotoService
{
    // Opens the OS photo picker/camera, then uploads the result to the API.
    // Returns the new public URL of the uploaded image, or null if cancelled/failed.
    Task<string?> PickAndUploadAsync(int userId);
}

// Implementation that lets the user choose a photo source, then streams
// the file to POST /upload/photo/{userId} on the FitLife API.
public class PhotoService : IPhotoService
{
    private readonly HttpClient _http;

    public PhotoService(HttpClient http)
    {
        _http = http;
    }

    // Shows an action sheet so the user can choose between Gallery and Camera.
    // Delegates to the appropriate MAUI MediaPicker API, then uploads the result.
    public async Task<string?> PickAndUploadAsync(int userId)
    {
        // Get the current page to anchor the action sheet dialog
        var page = Application.Current?.Windows[0].Page
                   ?? Application.Current?.MainPage;
        if (page is null) return null;

        var action = await page.DisplayActionSheet(
            "Profielfoto kiezen",
            "Annuleren",
            null,
            "Galerij / Bestanden",
            "Camera");

        // Route the selection to either camera capture or gallery picker
        FileResult? result = action switch
        {
            "Camera"              => await TryCapturePhotoAsync(),
            "Galerij / Bestanden" => await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                                     {
                                         Title = "Kies een profielfoto"
                                     }),
            _ => null  // User cancelled the action sheet
        };

        if (result is null) return null;

        return await UploadResultAsync(userId, result);
    }

    // Attempts to open the device camera. Shows an alert if the camera is unavailable.
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

    // Streams the selected photo file to the API as a multipart/form-data request.
    // The API saves the file, updates the user's photo_url in the database,
    // and returns the new public URL.
    private async Task<string?> UploadResultAsync(int userId, FileResult result)
    {
        await using var stream = await result.OpenReadAsync();
        using var content     = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);

        // Set the correct MIME type so the server can validate the file type
        var ext  = Path.GetExtension(result.FileName).ToLowerInvariant();
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

        // The API responds with { "photoUrl": "http://..." }
        var json = await response.Content.ReadFromJsonAsync<PhotoUploadResult>();
        return json?.PhotoUrl;
    }

    // Anonymous record to deserialise the API's upload response body
    private record PhotoUploadResult(string PhotoUrl);
}
