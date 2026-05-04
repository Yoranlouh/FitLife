using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers
{
    public static class WaitlistMapper
    {
        public static WaitlistResponse ToResponse(WaitlistEntry entry)
        {
            return new WaitlistResponse
            {
                Id = entry.Id,
                RequestDate = entry.RequestDate,
                Position = entry.Position,
                MemberId = entry.MemberId,
                MemberName = entry.Member != null ? $"{entry.Member.FirstName} {entry.Member.LastName}".Trim() : "Unknown",
                LessonId = entry.LessonId,
                WorkoutName = entry.Lesson?.Workout?.Name ?? "Unknown",
                LessonStartTime = entry.Lesson?.StartTime ?? default
            };
        }

        public static IEnumerable<WaitlistResponse> ToResponses(IEnumerable<WaitlistEntry> entries)
        {
            return entries.Select(ToResponse);
        }
    }
}
