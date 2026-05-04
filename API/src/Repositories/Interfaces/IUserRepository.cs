using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<ResultOf<IReadOnlyList<User>>> GetAllAsync();
        Task<ResultOf<User?>> GetByIdAsync(int id);
    }
}