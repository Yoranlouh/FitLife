using API.Domain.Common;
using API.Infrastructure.Database;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Implementations
{
    public class MemberRepository : IMemberRepository
    {
        private readonly ApiDbContext _db;

        public MemberRepository(ApiDbContext db)
        {
            _db = db;
        }

        public async Task<ResultOf<IReadOnlyList<Member>>> GetAllAsync()
        {
            try
            {
                var members = await _db.Members
                    .Include(m => m.Photo)
                    .Include(m => m.Subscription)
                    .AsNoTracking()
                    .ToListAsync();

                return ResultOf<IReadOnlyList<Member>>.Success(members);
            }
            catch (Exception ex)
            {
                return ResultOf<IReadOnlyList<Member>>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Member?>> GetByIdAsync(int id)
        {
            try
            {
                var member = await _db.Members
                    .Include(m => m.Photo)
                    .Include(m => m.Subscription)
                    .FirstOrDefaultAsync(m => m.Id == id);

                return ResultOf<Member?>.Success(member);
            }
            catch (Exception ex)
            {
                return ResultOf<Member?>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<Member>> AddAsync(Member member)
        {
            try
            {
                _db.Members.Add(member);
                await _db.SaveChangesAsync();
                return ResultOf<Member>.Success(member);
            }
            catch (Exception ex)
            {
                return ResultOf<Member>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> UpdateAsync(Member member)
        {
            try
            {
                _db.Members.Update(member);
                await _db.SaveChangesAsync();
                return ResultOf<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ResultOf<bool>.Failure(ex.Message);
            }
        }

        public async Task<ResultOf<bool>> DeleteAsync(int id)
        {
            try
            {
                var member = await _db.Members.FindAsync(id);
                if (member == null)
                {
                    return ResultOf<bool>.Failure("Member not found");
                }

                _db.Members.Remove(member);
                await _db.SaveChangesAsync();
                return ResultOf<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ResultOf<bool>.Failure(ex.Message);
            }
        }
    }
}
