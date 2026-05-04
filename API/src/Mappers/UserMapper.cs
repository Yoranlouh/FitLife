using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers
{
    public static class UserMapper
    {
        public static UserResponse ToResponse(User user)
        {
            return new UserResponse(user.Id, user.Name);
        }

        public static IEnumerable<UserResponse> ToResponses(IEnumerable<User> users)
        {
            return users.Select(ToResponse);
        }
    }
}
