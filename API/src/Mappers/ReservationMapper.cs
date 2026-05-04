using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers
{
    public static class ReservationMapper
    {
        public static ReservationResponse ToResponse(Reservation reservation)
        {
            return new ReservationResponse
            {
                Id = reservation.Id,
                ReservationDate = reservation.ReservationDate,
                IsCancelled = reservation.IsCancelled,
                MemberId = reservation.MemberId,
                MemberName = reservation.Member != null ? $"{reservation.Member.FirstName} {reservation.Member.LastName}".Trim() : "Unknown",
                LessonId = reservation.LessonId,
                WorkoutName = reservation.Lesson?.Workout?.Name ?? "Unknown",
                LessonStartTime = reservation.Lesson?.StartTime ?? default
            };
        }

        public static IEnumerable<ReservationResponse> ToResponses(IEnumerable<Reservation> reservations)
        {
            return reservations.Select(ToResponse);
        }
    }
}
