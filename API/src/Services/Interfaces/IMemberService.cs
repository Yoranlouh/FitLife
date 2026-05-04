using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Services.Interfaces
{
    public interface IMemberService
    {
        Task<ResultOf<IReadOnlyList<Member>>> GetAllMembersAsync();
        Task<ResultOf<Member?>> GetMemberByIdAsync(int id);
        Task<ResultOf<Member>> CreateMemberAsync(Member member);
        Task<ResultOf<bool>> UpdateMemberAsync(Member member);
        Task<ResultOf<bool>> DeleteMemberAsync(int id);
        Task<ResultOf<Member>> UpdateMemberPhotoAsync(int memberId, Stream photoStream, string contentType, string fileName);
    }
}
