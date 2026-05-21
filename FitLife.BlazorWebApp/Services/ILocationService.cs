namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for location management
/// </summary>
public interface ILocationService
{
    Task<List<LocationDto>> GetAllLocationsAsync();
    Task<LocationDto?> GetLocationByIdAsync(int locationId);
    Task<(bool Success, string Message)> CreateLocationAsync(LocationDto location);
    Task<(bool Success, string Message)> UpdateLocationAsync(LocationDto location);
    Task<(bool Success, string Message)> DeleteLocationAsync(int locationId);
}