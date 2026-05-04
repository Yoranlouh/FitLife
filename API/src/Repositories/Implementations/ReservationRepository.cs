using API.Domain.Common;
using API.Infrastructure.Database;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;
using System.Globalization;

namespace API.Repositories.Implementations
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApiDbContext _db;

        public ReservationRepository(ApiDbContext db)
        {
            _db = db;
        }

        public async Task<ResultOf<IReadOnlyList<Reservation>>> GetAllAsync()
        {
            try
            {
                var reservations = await _db.Reservations
                    .Include(r => r.Member)
                    .Include(r => r.Lesson)
                    .AsNoTracking()
                    .ToListAsync();

                return ResultOf<IReadOnlyList<Reservation>>.Success(reservations);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<Reservation>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Reservation?>> GetByIdAsync(int id)
        {
            try
            {
                var reservation = await _db.Reservations
                    .Include(r => r.Member)
                    .Include(r => r.Lesson)
                    .FirstOrDefaultAsync(r => r.Id == id);

                return ResultOf<Reservation?>.Success(reservation);
            }
            catch (Exception ex)
            {
                return ResultOf<Reservation?>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<IReadOnlyList<Reservation>>> GetByMemberIdAsync(int memberId)
        {
            try
            {
                var reservations = await _db.Reservations
                    .Where(r => r.MemberId == memberId)
                    .Include(r => r.Lesson)
                    .ThenInclude(l => l.Workout)
                    .AsNoTracking()
                    .ToListAsync();

                return ResultOf<IReadOnlyList<Reservation>>.Success(reservations);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<Reservation>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<IReadOnlyList<Reservation>>> GetByLessonIdAsync(int lessonId)
        {
            try
            {
                var reservations = await _db.Reservations
                    .Where(r => r.LessonId == lessonId)
                    .AsNoTracking()
                    .ToListAsync();

                return ResultOf<IReadOnlyList<Reservation>>.Success(reservations);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<Reservation>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Reservation>> AddAsync(Reservation reservation)
        {
            try
            {
                _db.Reservations.Add(reservation);
                await _db.SaveChangesAsync();
                return ResultOf<Reservation>.Success(reservation);
            }
            catch (Exception ex)
            {
                return ResultOf<Reservation>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> UpdateAsync(Reservation reservation)
        {
            try
            {
                _db.Reservations.Update(reservation);
                await _db.SaveChangesAsync();
                return ResultOf<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ResultOf<bool>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> DeleteAsync(int id)
        {
            try
            {
                var reservation = await _db.Reservations.FindAsync(id);
                if (reservation == null)
                {
                    return ResultOf<bool>.Failure("Reservation not found");
                }

                _db.Reservations.Remove(reservation);
                await _db.SaveChangesAsync();
                return ResultOf<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ResultOf<bool>.Failure(ex.Message);
            }
        }

        public async Task<int> GetWeeklyCountForMemberAsync(int memberId, DateTime dateInWeek)
        {
            // ISO 8601 week starts on Monday
            var day = (int)dateInWeek.DayOfWeek;
            var diff = (day == 0 ? 7 : day) - 1;
            var startOfWeek = dateInWeek.AddDays(-1 * diff).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            return await _db.Reservations
                .CountAsync(r => r.MemberId == memberId && 
                                 !r.IsCancelled &&
                                 r.Lesson.StartTime >= startOfWeek && 
                                 r.Lesson.StartTime < endOfWeek);
        }

        public async Task<bool> HasReservationForLessonAsync(int memberId, int lessonId)
        {
            return await _db.Reservations
                .AnyAsync(r => r.MemberId == memberId && r.LessonId == lessonId && !r.IsCancelled);
        }
    }
}
