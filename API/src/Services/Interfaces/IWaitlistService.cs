using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Services.Interfaces
{
    public interface IWaitlistService
    {
        Task<ResultOf<WaitlistEntry>> JoinWaitlistAsync(int memberId, int lessonId);
        Task<ResultOf<bool>> LeaveWaitlistAsync(int id);
        Task<ResultOf<IReadOnlyList<WaitlistEntry>>> GetWaitlistForLessonAsync(int lessonId);
        Task<ResultOf<IReadOnlyList<WaitlistEntry>>> GetMemberWaitlistEntriesAsync(int memberId);
        Task<ResultOf<WaitlistEntry?>> GetWaitlistEntryByIdAsync(int id);
        Task<ResultOf<bool>> TriggerNotificationAsync(int lessonId);
    }
}
