using API.Domain.Common;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using API.Storage.Interfaces;
using SharedLibrary.Domain.Entities;

namespace API.Services.Implementations
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _repository;
        private readonly IPhotoStorage _photoStorage;

        public MemberService(IMemberRepository repository, IPhotoStorage photoStorage)
        {
            _repository = repository;
            _photoStorage = photoStorage;
        }

        public async Task<ResultOf<IReadOnlyList<Member>>> GetAllMembersAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ResultOf<Member?>> GetMemberByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ResultOf<Member>> CreateMemberAsync(Member member)
        {
            member.JoinDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(member.Status))
            {
                member.Status = "Active";
            }
            return await _repository.AddAsync(member);
        }

        public async Task<ResultOf<bool>> UpdateMemberAsync(Member member)
        {
            return await _repository.UpdateAsync(member);
        }

        public async Task<ResultOf<bool>> DeleteMemberAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<ResultOf<Member>> UpdateMemberPhotoAsync(int memberId, Stream photoStream, string contentType, string fileName)
        {
            var memberResult = await _repository.GetByIdAsync(memberId);
            if (memberResult.IsFailure) return ResultOf<Member>.Failure(memberResult.Error);
            if (memberResult.Value == null) return ResultOf<Member>.Failure("Member not found");

            var member = memberResult.Value;

            try
            {
                var saveResult = await _photoStorage.SaveAsync(
                    photoStream, 
                    contentType, 
                    fileName, 
                    "members", 
                    CancellationToken.None, 
                    memberId
                );

                member.PhotoId = int.Parse(saveResult.Id);
                await _repository.UpdateAsync(member);

                // Reload to get Photo object
                var finalResult = await _repository.GetByIdAsync(memberId);
                return ResultOf<Member>.Success(finalResult.Value!);
            }
            catch (Exception ex)
            {
                return ResultOf<Member>.Failure(ex.Message);
            }
        }
    }
}
