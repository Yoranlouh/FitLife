using System.Net.Http.Json;
using SharedLibrary.DTOs.Responses;
using WA.Services.Http.Interfaces;

namespace WA.Services.Http;

public sealed class LocationApi : ILocationApi
{
    private readonly HttpClient _http;

    public LocationApi(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<LocationResponse>> GetLocationsAsync(CancellationToken ct = default)
    {
        var locations = await _http.GetFromJsonAsync<List<LocationResponse>>("api/locations", ct);
        return locations ?? [];
    }
}
