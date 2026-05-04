using System.Net.Http.Json;
using SharedLibrary.DTOs.Responses;
using WA.Services.Http.Interfaces;

namespace WA.Services.Http;

public sealed class ReservationApi : IReservationApi
{
    private readonly HttpClient _http;

    public ReservationApi(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<ReservationResponse>> GetReservationsAsync(CancellationToken ct = default)
    {
        var reservations = await _http.GetFromJsonAsync<List<ReservationResponse>>("api/reservations", ct);
        return reservations ?? [];
    }

    public async Task<IReadOnlyList<ReservationResponse>> GetReservationsByMemberAsync(int memberId, CancellationToken ct = default)
    {
        var reservations = await _http.GetFromJsonAsync<List<ReservationResponse>>($"api/reservations/member/{memberId}", ct);
        return reservations ?? [];
    }
}
