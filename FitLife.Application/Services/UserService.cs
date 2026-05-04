using FitLife.Application.DTOs;
using FitLife.Domain.Entities;
using FitLife.Domain.Interfaces;

namespace FitLife.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : new UserDto(user.Id, user.Name, user.Email);
    }
}
