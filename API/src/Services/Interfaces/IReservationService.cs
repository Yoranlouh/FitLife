using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Services.Interfaces
{
    public interface IReservationService
    {
        Task<ResultOf<IReadOnlyList<Reservation>>> GetAllReservationsAsync();
        Task<ResultOf<Reservation?>> GetReservationByIdAsync(int id);
        Task<ResultOf<IReadOnlyList<Reservation>>> GetMemberReservationsAsync(int memberId);
        Task<ResultOf<Reservation>> CreateReservationAsync(int memberId, int lessonId);
        Task<ResultOf<bool>> CancelReservationAsync(int id);
    }
}
