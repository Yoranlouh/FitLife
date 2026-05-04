using API.Domain.Common;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using SharedLibrary.Domain.Entities;

namespace API.Services.Implementations
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _repository;

        public LessonService(ILessonRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResultOf<IReadOnlyList<Lesson>>> GetAllLessonsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ResultOf<Lesson?>> GetLessonByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ResultOf<Lesson>> CreateLessonAsync(Lesson lesson)
        {
            if (!lesson.IsRecurring)
            {
                return await _repository.AddAsync(lesson);
            }

            // Herhalingslogica
            var lessons = new List<Lesson> { lesson };
            var currentStartTime = lesson.StartTime;
            var currentEndTime = lesson.EndTime;
            var count = 1;

            while (ShouldCreateNext(lesson, currentStartTime, count))
            {
                currentStartTime = GetNextDate(currentStartTime, lesson.RecurrencePattern, lesson.RecurrenceInterval ?? 1);
                currentEndTime = GetNextDate(currentEndTime, lesson.RecurrencePattern, lesson.RecurrenceInterval ?? 1);
                
                var nextLesson = new Lesson
                {
                    StartTime = currentStartTime,
                    EndTime = currentEndTime,
                    MaxParticipants = lesson.MaxParticipants,
                    WorkoutId = lesson.WorkoutId,
                    InstructorId = lesson.InstructorId,
                    LocationId = lesson.LocationId,
                    IsRecurring = true,
                    RecurrencePattern = lesson.RecurrencePattern,
                    RecurrenceInterval = lesson.RecurrenceInterval,
                    RecurrenceEndDate = lesson.RecurrenceEndDate,
                    RecurrenceCount = lesson.RecurrenceCount,
                    ParentLesson = lesson
                };
                
                lessons.Add(nextLesson);
                count++;
                
                // Veiligheidsstop om oneindige lussen te voorkomen
                if (count > 100) break;
            }

            var result = await _repository.AddRangeAsync(lessons);
            if (result.IsFailure) return ResultOf<Lesson>.Failure(result.Error);

            return ResultOf<Lesson>.Success(lesson);
        }

        private bool ShouldCreateNext(Lesson lesson, DateTime currentStartTime, int count)
        {
            if (lesson.RecurrenceEndDate.HasValue)
            {
                var nextDate = GetNextDate(currentStartTime, lesson.RecurrencePattern, lesson.RecurrenceInterval ?? 1);
                return nextDate <= lesson.RecurrenceEndDate.Value;
            }

            if (lesson.RecurrenceCount.HasValue)
            {
                return count < lesson.RecurrenceCount.Value;
            }

            return false;
        }

        private DateTime GetNextDate(DateTime current, string? pattern, int interval)
        {
            return pattern?.ToLower() switch
            {
                "daily" => current.AddDays(interval),
                "weekly" => current.AddDays(7 * interval),
                "monthly" => current.AddMonths(interval),
                _ => current.AddDays(interval)
            };
        }

        public async Task<ResultOf<bool>> UpdateLessonAsync(Lesson lesson)
        {
            return await _repository.UpdateAsync(lesson);
        }

        public async Task<ResultOf<bool>> DeleteLessonAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}