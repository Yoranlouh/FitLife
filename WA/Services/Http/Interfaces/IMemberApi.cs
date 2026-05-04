using SharedLibrary.DTOs.Responses;

namespace WA.Services.Http.Interfaces;

public interface IMemberApi
{
    Task<IReadOnlyList<MemberResponse>> GetMembersAsync(CancellationToken ct = default);
    Task<MemberResponse?> GetMemberByIdAsync(int id, CancellationToken ct = default);
}
