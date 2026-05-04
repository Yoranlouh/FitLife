using SharedLibrary.DTOs.Responses;

namespace WA.Services.Http.Interfaces;

public interface IReservationApi
{
    Task<IReadOnlyList<ReservationResponse>> GetReservationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReservationResponse>> GetReservationsByMemberAsync(int memberId, CancellationToken ct = default);
}
