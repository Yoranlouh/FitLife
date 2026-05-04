using API.Domain.Common;
using SharedLibrary.Domain.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResultOf<IReadOnlyList<User>>> GetAllUsersAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ResultOf<User?>> GetUserByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }
    }
}
