using API.Domain.Common;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using SharedLibrary.Domain.Entities;

namespace API.Services.Implementations
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IMemberRepository _memberRepository;

        public ReservationService(
            IReservationRepository reservationRepository,
            ILessonRepository lessonRepository,
            IMemberRepository memberRepository)
        {
            _reservationRepository = reservationRepository;
            _lessonRepository = lessonRepository;
            _memberRepository = memberRepository;
        }

        public async Task<ResultOf<IReadOnlyList<Reservation>>> GetAllReservationsAsync()
        {
            return await _reservationRepository.GetAllAsync();
        }

        public async Task<ResultOf<Reservation?>> GetReservationByIdAsync(int id)
        {
            return await _reservationRepository.GetByIdAsync(id);
        }

        public async Task<ResultOf<IReadOnlyList<Reservation>>> GetMemberReservationsAsync(int memberId)
        {
            return await _reservationRepository.GetByMemberIdAsync(memberId);
        }

        public async Task<ResultOf<Reservation>> CreateReservationAsync(int memberId, int lessonId)
        {
            // 1. Check of de les bestaat
            var lessonResult = await _lessonRepository.GetByIdAsync(lessonId);
            if (lessonResult.IsFailure || lessonResult.Value == null)
            {
                return ResultOf<Reservation>.Failure("Les niet gevonden.");
            }
            var lesson = lessonResult.Value;

            // 2. Check of het lid bestaat
            var memberResult = await _memberRepository.GetByIdAsync(memberId);
            if (memberResult.IsFailure || memberResult.Value == null)
            {
                return ResultOf<Reservation>.Failure("Lid niet gevonden.");
            }

            // 3. Max 1 week vooruit check
            var now = DateTime.Now;
            var oneWeekFromNow = now.AddDays(7);
            if (lesson.StartTime > oneWeekFromNow)
            {
                return ResultOf<Reservation>.Failure("Je kunt maximaal 1 week vooruit reserveren.");
            }
            if (lesson.StartTime < now)
            {
                return ResultOf<Reservation>.Failure("Je kunt geen reservering maken voor een les die al is begonnen of afgelopen.");
            }

            // 4. Max 2x per week limiet check
            var weeklyCount = await _reservationRepository.GetWeeklyCountForMemberAsync(memberId, lesson.StartTime);
            if (weeklyCount >= 2)
            {
                return ResultOf<Reservation>.Failure("Je hebt je limiet van 2 reserveringen per week bereikt.");
            }

            // 5. Dubbele reservering check
            var hasExisting = await _reservationRepository.HasReservationForLessonAsync(memberId, lessonId);
            if (hasExisting)
            {
                return ResultOf<Reservation>.Failure("Je hebt al een reservering voor deze les.");
            }

            // 6. Capaciteit check
            var currentReservationsResult = await _reservationRepository.GetByLessonIdAsync(lessonId);
            if (currentReservationsResult.IsSuccess)
            {
                var activeReservationsCount = currentReservationsResult.Value!.Count(r => !r.IsCancelled);
                if (activeReservationsCount >= lesson.MaxParticipants)
                {
                    return ResultOf<Reservation>.Failure("Deze les is volgeboekt.");
                }
            }

            // 7. Reservering maken
            var reservation = new Reservation
            {
                MemberId = memberId,
                LessonId = lessonId,
                ReservationDate = now,
                IsCancelled = false
            };

            return await _reservationRepository.AddAsync(reservation);
        }

        public async Task<ResultOf<bool>> CancelReservationAsync(int id)
        {
            var reservationResult = await _reservationRepository.GetByIdAsync(id);
            if (reservationResult.IsFailure || reservationResult.Value == null)
            {
                return ResultOf<bool>.Failure("Reservering niet gevonden.");
            }

            var reservation = reservationResult.Value;
            
            // Check of de les nog niet begonnen is
            if (reservation.Lesson.StartTime <= DateTime.Now)
            {
                return ResultOf<bool>.Failure("Je kunt een reservering niet annuleren als de les al is begonnen.");
            }

            reservation.IsCancelled = true;
            return await _reservationRepository.UpdateAsync(reservation);
        }
    }
}
