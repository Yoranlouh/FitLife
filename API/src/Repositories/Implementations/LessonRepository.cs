using API.Domain.Common;
using API.Infrastructure.Database;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Implementations
{
    public class LessonRepository : ILessonRepository
    {
        private readonly ApiDbContext _db;

        public LessonRepository(ApiDbContext db)
        {
            _db = db;
        }

        public async Task<ResultOf<IReadOnlyList<Lesson>>> GetAllAsync()
        {
            try
            {
                var lessons = await _db.Lessons
                    .Include(l => l.Workout)
                    .Include(l => l.Instructor)
                    .Include(l => l.Location)
                    .AsNoTracking()
                    .ToListAsync();

                return ResultOf<IReadOnlyList<Lesson>>.Success(lessons);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<Lesson>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Lesson?>> GetByIdAsync(int id)
        {
            try
            {
                var lesson = await _db.Lessons
                    .Include(l => l.Workout)
                    .Include(l => l.Instructor)
                    .Include(l => l.Location)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == id);

                return ResultOf<Lesson?>.Success(lesson);
            }
            catch (Exception ex)
            {
                return ResultOf<Lesson?>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Lesson>> AddAsync(Lesson lesson)
        {
            try
            {
                _db.Lessons.Add(lesson);
                await _db.SaveChangesAsync();
                return ResultOf<Lesson>.Success(lesson);
            }
            catch (Exception ex)
            {
                return ResultOf<Lesson>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<IEnumerable<Lesson>>> AddRangeAsync(IEnumerable<Lesson> lessons)
        {
            try
            {
                _db.Lessons.AddRange(lessons);
                await _db.SaveChangesAsync();
                return ResultOf<IEnumerable<Lesson>>.Success(lessons);
            }
            catch (Exception ex)
            {
                return ResultOf<IEnumerable<Lesson>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> UpdateAsync(Lesson lesson)
        {
            try
            {
                _db.Lessons.Update(lesson);
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
                var lesson = await _db.Lessons.FindAsync(id);
                if (lesson == null)
                {
                    return ResultOf<bool>.Failure("Lesson not found");
                }

                _db.Lessons.Remove(lesson);
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