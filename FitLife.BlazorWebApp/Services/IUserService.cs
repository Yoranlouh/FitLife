using FitLife.BlazorWebApp.Models;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for user management (admin only)
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves all users, optionally filtered by role (member, instructor, admin)
    /// </summary>
    Task<List<MemberDto>> GetAllUsersAsync(string? roleFilter = null);

    /// <summary>
    /// Gets a single user by their unique ID
    /// </summary>
    Task<MemberDto?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Updates a user's role (member, instructor, admin)
    /// </summary>
    Task<(bool Success, string Message)> UpdateUserRoleAsync(int userId, string newRole);
}