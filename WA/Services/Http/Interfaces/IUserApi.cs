using SharedLibrary.DTOs.Responses;

namespace WA.Services.Http.Interfaces;

public interface IUserApi
{
    Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken ct = default);
}