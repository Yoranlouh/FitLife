using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Services.Interfaces
{
    public interface IUserService
    {
        Task<ResultOf<IReadOnlyList<User>>> GetAllUsersAsync();
        Task<ResultOf<User?>> GetUserByIdAsync(int id);
    }
}
