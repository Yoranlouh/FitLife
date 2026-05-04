using API.Domain.Common;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;
using API.Infrastructure.Database;
using API.Repositories.Interfaces;

namespace API.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly ApiDbContext _db;

    public UserRepository(ApiDbContext db)
    {
        _db = db;
    }

    public async Task<ResultOf<IReadOnlyList<User>>> GetAllAsync()
    {
        try
        {
            var users = await _db.Users
                .AsNoTracking()
                .ToListAsync();

            return ResultOf<IReadOnlyList<User>>.Success(users);
        }
        catch (Exception ex)
        {
            return ResultOf<IReadOnlyList<User>>.Failure(ex.Message);
        }
    }

    public async Task<ResultOf<User?>> GetByIdAsync(int id)
    {
        try
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ResultOf<User?>.Failure("User not found");

            return ResultOf<User?>.Success(user);
        }
        catch (Exception ex)
        {
            return ResultOf<User?>.Failure(ex.Message);
        }
    }
}