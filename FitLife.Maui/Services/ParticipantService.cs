using SharedLibrary.DTOs.Responses;
using System.Net.Http.Json;

namespace FitLife.Maui.Services;

public interface IParticipantService
{
    Task<IEnumerable<ParticipantResponse>> GetParticipantsAsync(int lessonId);
    Task<IEnumerable<ParticipantResponse>> GetWaitlistAsync(int lessonId);
}

public class ParticipantService : IParticipantService
{
    private readonly HttpClient _httpClient;

    public ParticipantService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ParticipantResponse>> GetParticipantsAsync(int lessonId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"lessons/{lessonId}/participants");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<ParticipantResponse>>()
                       ?? Enumerable.Empty<ParticipantResponse>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching participants: {ex.Message}");
        }

        return Enumerable.Empty<ParticipantResponse>();
    }

    public async Task<IEnumerable<ParticipantResponse>> GetWaitlistAsync(int lessonId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"lessons/{lessonId}/waitlist");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<ParticipantResponse>>()
                       ?? Enumerable.Empty<ParticipantResponse>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching waitlist: {ex.Message}");
        }

        return Enumerable.Empty<ParticipantResponse>();
    }
}