using API.Domain.Common;
using API.Infrastructure.Database;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Implementations
{
    public class InstructorRepository : IInstructorRepository
    {
        private readonly ApiDbContext _db;

        public InstructorRepository(ApiDbContext db)
        {
            _db = db;
        }

        public async Task<ResultOf<IReadOnlyList<Instructor>>> GetAllAsync()
        {
            try
            {
                var instructors = await _db.Instructors
                    .Include(i => i.Photo)
                    .AsNoTracking()
                    .ToListAsync();

                return ResultOf<IReadOnlyList<Instructor>>.Success(instructors);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<Instructor>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Instructor?>> GetByIdAsync(int id)
        {
            try
            {
                var instructor = await _db.Instructors
                    .Include(i => i.Photo)
                    .FirstOrDefaultAsync(i => i.Id == id);

                return ResultOf<Instructor?>.Success(instructor);
            }
            catch (Exception ex)
            {
                return ResultOf<Instructor?>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Instructor>> AddAsync(Instructor instructor)
        {
            try
            {
                _db.Instructors.Add(instructor);
                await _db.SaveChangesAsync();
                return ResultOf<Instructor>.Success(instructor);
            }
            catch (Exception ex)
            {
                return ResultOf<Instructor>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> UpdateAsync(Instructor instructor)
        {
            try
            {
                _db.Instructors.Update(instructor);
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
                var instructor = await _db.Instructors.FindAsync(id);
                if (instructor == null)
                {
                    return ResultOf<bool>.Failure("Instructor not found");
                }

                _db.Instructors.Remove(instructor);
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
