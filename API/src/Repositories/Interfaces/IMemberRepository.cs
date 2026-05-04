using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Interfaces
{
    public interface IMemberRepository
    {
        Task<ResultOf<IReadOnlyList<Member>>> GetAllAsync();
        Task<ResultOf<Member?>> GetByIdAsync(int id);
        Task<ResultOf<Member>> AddAsync(Member member);
        Task<ResultOf<bool>> UpdateAsync(Member member);
        Task<ResultOf<bool>> DeleteAsync(int id);
    }
}
