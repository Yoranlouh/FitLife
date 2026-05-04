using SharedLibrary.Domain.Entities;
using API.Domain.Common;

namespace API.Repositories.Interfaces
{
    public interface IReservationRepository
    {
        Task<ResultOf<IReadOnlyList<Reservation>>> GetAllAsync();
        Task<ResultOf<Reservation?>> GetByIdAsync(int id);
        Task<ResultOf<IReadOnlyList<Reservation>>> GetByMemberIdAsync(int memberId);
        Task<ResultOf<IReadOnlyList<Reservation>>> GetByLessonIdAsync(int lessonId);
        Task<ResultOf<Reservation>> AddAsync(Reservation reservation);
        Task<ResultOf<bool>> UpdateAsync(Reservation reservation);
        Task<ResultOf<bool>> DeleteAsync(int id);
        Task<int> GetWeeklyCountForMemberAsync(int memberId, DateTime dateInWeek);
        Task<bool> HasReservationForLessonAsync(int memberId, int lessonId);
    }
}
