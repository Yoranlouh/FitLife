using SharedLibrary.DTOs.Responses;

namespace WA.Services.Http.Interfaces;

public interface ILocationApi
{
    Task<IReadOnlyList<LocationResponse>> GetLocationsAsync(CancellationToken ct = default);
}
