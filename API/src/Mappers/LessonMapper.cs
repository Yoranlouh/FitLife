using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers
{
    public static class LessonMapper
    {
        public static LessonResponse ToResponse(Lesson lesson)
        {
            return new LessonResponse
            {
                Id = lesson.Id,
                StartTime = lesson.StartTime,
                EndTime = lesson.EndTime,
                MaxParticipants = lesson.MaxParticipants,
                WorkoutId = lesson.WorkoutId,
                WorkoutName = lesson.Workout?.Name ?? "Unknown",
                InstructorId = lesson.InstructorId,
                InstructorName = lesson.Instructor != null ? $"{lesson.Instructor.FirstName} {lesson.Instructor.LastName}".Trim() : "Unknown",
                LocationId = lesson.LocationId,
                LocationName = lesson.Location?.Name ?? "Unknown",
                IsRecurring = lesson.IsRecurring,
                RecurrencePattern = lesson.RecurrencePattern,
                RecurrenceInterval = lesson.RecurrenceInterval,
                RecurrenceEndDate = lesson.RecurrenceEndDate,
                RecurrenceCount = lesson.RecurrenceCount,
                ParentLessonId = lesson.ParentLessonId
            };
        }

        public static IEnumerable<LessonResponse> ToResponses(IEnumerable<Lesson> lessons)
        {
            return lessons.Select(ToResponse);
        }

        public static Lesson ToEntity(LessonCreateRequest request)
        {
            return new Lesson
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                MaxParticipants = request.MaxParticipants,
                WorkoutId = request.WorkoutId,
                InstructorId = request.InstructorId,
                LocationId = request.LocationId,
                IsRecurring = request.IsRecurring,
                RecurrencePattern = request.RecurrencePattern,
                RecurrenceInterval = request.RecurrenceInterval,
                RecurrenceEndDate = request.RecurrenceEndDate,
                RecurrenceCount = request.RecurrenceCount
            };
        }

        public static void UpdateEntity(Lesson lesson, LessonUpdateRequest request)
        {
            lesson.StartTime = request.StartTime;
            lesson.EndTime = request.EndTime;
            lesson.MaxParticipants = request.MaxParticipants;
            lesson.WorkoutId = request.WorkoutId;
            lesson.InstructorId = request.InstructorId;
            lesson.LocationId = request.LocationId;
        }
    }
}