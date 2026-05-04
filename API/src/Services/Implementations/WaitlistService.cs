using API.Domain.Common;
using API.Infrastructure.Database;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;

namespace API.Services.Implementations
{
    public class WaitlistService : IWaitlistService
    {
        private readonly ApiDbContext _context;

        public WaitlistService(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<ResultOf<WaitlistEntry>> JoinWaitlistAsync(int memberId, int lessonId)
        {
            try
            {
                var lesson = await _context.Lessons
                    .Include(l => l.WaitlistEntries)
                    .FirstOrDefaultAsync(l => l.Id == lessonId);

                if (lesson == null)
                    return ResultOf<WaitlistEntry>.Failure("Lesson not found");

                var member = await _context.Members.FindAsync(memberId);
                if (member == null)
                    return ResultOf<WaitlistEntry>.Failure("Member not found");

                var existingEntry = await _context.WaitlistEntries
                    .FirstOrDefaultAsync(w => w.LessonId == lessonId && w.MemberId == memberId);

                if (existingEntry != null)
                    return ResultOf<WaitlistEntry>.Failure("Member already on waitlist for this lesson");

                // Check if already reserved
                var existingReservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.LessonId == lessonId && r.MemberId == memberId && !r.IsCancelled);
                
                if (existingReservation != null)
                    return ResultOf<WaitlistEntry>.Failure("Member already has a reservation for this lesson");

                int nextPosition = lesson.WaitlistEntries.Count + 1;

                var entry = new WaitlistEntry
                {
                    MemberId = memberId,
                    LessonId = lessonId,
                    RequestDate = DateTime.UtcNow,
                    Position = nextPosition
                };

                _context.WaitlistEntries.Add(entry);
                await _context.SaveChangesAsync();

                return ResultOf<WaitlistEntry>.Success(entry);
            }
            catch (Exception ex)
            {
                return ResultOf<WaitlistEntry>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> LeaveWaitlistAsync(int id)
        {
            try
            {
                var entry = await _context.WaitlistEntries.FindAsync(id);
                if (entry == null)
                    return ResultOf<bool>.Failure("Waitlist entry not found");

                int lessonId = entry.LessonId;
                int position = entry.Position;

                _context.WaitlistEntries.Remove(entry);
                
                // Update positions for others
                var laterEntries = await _context.WaitlistEntries
                    .Where(w => w.LessonId == lessonId && w.Position > position)
                    .ToListAsync();

                foreach (var laterEntry in laterEntries)
                {
                    laterEntry.Position--;
                }

                await _context.SaveChangesAsync();
                return ResultOf<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ResultOf<bool>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<IReadOnlyList<WaitlistEntry>>> GetWaitlistForLessonAsync(int lessonId)
        {
            try
            {
                var entries = await _context.WaitlistEntries
                    .Include(w => w.Member)
                    .Include(w => w.Lesson)
                        .ThenInclude(l => l.Workout)
                    .Where(w => w.LessonId == lessonId)
                    .OrderBy(w => w.Position)
                    .ToListAsync();

                return ResultOf<IReadOnlyList<WaitlistEntry>>.Success(entries);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<WaitlistEntry>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<IReadOnlyList<WaitlistEntry>>> GetMemberWaitlistEntriesAsync(int memberId)
        {
            try
            {
                var entries = await _context.WaitlistEntries
                    .Include(w => w.Member)
                    .Include(w => w.Lesson)
                        .ThenInclude(l => l.Workout)
                    .Where(w => w.MemberId == memberId)
                    .OrderBy(w => w.RequestDate)
                    .ToListAsync();

                return ResultOf<IReadOnlyList<WaitlistEntry>>.Success(entries);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<WaitlistEntry>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<WaitlistEntry?>> GetWaitlistEntryByIdAsync(int id)
        {
            try
            {
                var entry = await _context.WaitlistEntries
                    .Include(w => w.Member)
                    .Include(w => w.Lesson)
                        .ThenInclude(l => l.Workout)
                    .FirstOrDefaultAsync(w => w.Id == id);

                return ResultOf<WaitlistEntry?>.Success(entry);
            }
            catch (Exception ex)
            {
                return ResultOf<WaitlistEntry?>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> TriggerNotificationAsync(int lessonId)
        {
            try
            {
                var entries = await _context.WaitlistEntries
                    .Include(w => w.Member)
                    .Include(w => w.Lesson)
                        .ThenInclude(l => l.Workout)
                    .Where(w => w.LessonId == lessonId)
                    .OrderBy(w => w.Position)
                    .ToListAsync();

                if (!entries.Any())
                    return ResultOf<bool>.Success(false);

                // For the first person on the waitlist, simulate notification
                var firstEntry = entries.First();
                
                Console.WriteLine($"[NOTIFICATIE] Lid {firstEntry.Member.FirstName} {firstEntry.Member.LastName} ({firstEntry.Member.Email}): " +
                                  $"Er is een plek vrijgekomen voor de les {firstEntry.Lesson.Workout.Name} op {firstEntry.Lesson.StartTime:dd-MM-yyyy HH:mm}!");

                return ResultOf<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ResultOf<bool>.Failure(ex.Message);
            }
        }
    }
}
