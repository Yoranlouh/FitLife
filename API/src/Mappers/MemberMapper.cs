using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers
{
    public static class MemberMapper
    {
        public static MemberResponse ToResponse(Member member)
        {
            return new MemberResponse
            {
                Id = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                Username = member.Username,
                ProfileDescription = member.ProfileDescription,
                Status = member.Status,
                JoinDate = member.JoinDate,
                PhotoUrl = member.Photo?.Url,
                SubscriptionId = member.SubscriptionId,
                SubscriptionName = member.Subscription?.Name
            };
        }

        public static IEnumerable<MemberResponse> ToResponses(IEnumerable<Member> members)
        {
            return members.Select(ToResponse);
        }

        public static Member ToEntity(MemberCreateRequest request)
        {
            return new Member
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Username = request.Username,
                ProfileDescription = request.ProfileDescription,
                SubscriptionId = request.SubscriptionId
            };
        }

        public static void UpdateEntity(Member member, MemberUpdateRequest request)
        {
            if (request.FirstName != null) member.FirstName = request.FirstName;
            if (request.LastName != null) member.LastName = request.LastName;
            if (request.Email != null) member.Email = request.Email;
            if (request.Username != null) member.Username = request.Username;
            if (request.ProfileDescription != null) member.ProfileDescription = request.ProfileDescription;
            if (request.Status != null) member.Status = request.Status;
            if (request.SubscriptionId != null) member.SubscriptionId = request.SubscriptionId;
        }
    }
}
