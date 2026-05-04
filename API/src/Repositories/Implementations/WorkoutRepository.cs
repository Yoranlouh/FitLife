using API.Domain.Common;
using API.Infrastructure.Database;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Implementations
{
    public class WorkoutRepository : IWorkoutRepository
    {
        private readonly ApiDbContext _db;

        public WorkoutRepository(ApiDbContext db)
        {
            _db = db;
        }

        public async Task<ResultOf<IReadOnlyList<Workout>>> GetAllAsync()
        {
            try
            {
                var workouts = await _db.Workouts
                    .AsNoTracking()
                    .ToListAsync();

                return ResultOf<IReadOnlyList<Workout>>.Success(workouts);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<Workout>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Workout?>> GetByIdAsync(int id)
        {
            try
            {
                var workout = await _db.Workouts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == id);

                return ResultOf<Workout?>.Success(workout);
            }
            catch (Exception ex)
            {
                return ResultOf<Workout?>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Workout>> AddAsync(Workout workout)
        {
            try
            {
                _db.Workouts.Add(workout);
                await _db.SaveChangesAsync();
                return ResultOf<Workout>.Success(workout);
            }
            catch (Exception ex)
            {
                return ResultOf<Workout>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> UpdateAsync(Workout workout)
        {
            try
            {
                _db.Workouts.Update(workout);
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
                var workout = await _db.Workouts.FindAsync(id);
                if (workout == null)
                {
                    return ResultOf<bool>.Failure("Workout not found");
                }

                _db.Workouts.Remove(workout);
                await _db.SaveChangesAsync();
                return ResultOf<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ResultOf<bool>.Failure(ex.Message);
            }
        }
    }
}
