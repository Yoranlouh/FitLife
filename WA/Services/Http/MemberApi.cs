using System.Net.Http.Json;
using SharedLibrary.DTOs.Responses;
using WA.Services.Http.Interfaces;

namespace WA.Services.Http;

public sealed class MemberApi : IMemberApi
{
    private readonly HttpClient _http;

    public MemberApi(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<MemberResponse>> GetMembersAsync(CancellationToken ct = default)
    {
        var members = await _http.GetFromJsonAsync<List<MemberResponse>>("api/members", ct);
        return members ?? [];
    }

    public async Task<MemberResponse?> GetMemberByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<MemberResponse>($"api/members/{id}", ct);
    }
}
