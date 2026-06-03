using SharedLibrary.DTOs.Responses;
using System.Net.Http.Json;

namespace FitLife.Maui.Services;

// Interface for fetching lesson participants and the waitlist from the API.
public interface IParticipantService
{
    // Returns all confirmed participants for the given lesson (non-cancelled reservations).
    Task<IEnumerable<ParticipantResponse>> GetParticipantsAsync(int lessonId);

    // Returns all users currently on the waitlist for the given lesson.
    Task<IEnumerable<ParticipantResponse>> GetWaitlistAsync(int lessonId);
}

// HTTP implementation that talks to the FitLife REST API.
public class ParticipantService : IParticipantService
{
    private readonly HttpClient _httpClient;

    public ParticipantService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Calls GET /lessons/{lessonId}/participants.
    // Returns an empty list on network error so the UI degrades gracefully.
    public async Task<IEnumerable<ParticipantResponse>> GetParticipantsAsync(int lessonId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"lessons/{lessonId}/participants");

            if (response.IsSuccessStatusCode)
            {
                // Deserialise the JSON array into a list of ParticipantResponse objects
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

    // Calls GET /lessons/{lessonId}/waitlist.
    // Returns an empty list on network error.
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
