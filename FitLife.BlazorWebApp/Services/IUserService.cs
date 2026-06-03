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

    /// <summary>
    /// Deletes a user and all their associated data
    /// </summary>
    Task<(bool Success, string Message)> DeleteUserAsync(int userId);

    /// <summary>
    /// Pauses or resumes a user's subscription
    /// </summary>
    Task<(bool Success, string Message)> PauseSubscriptionAsync(int userId, bool pause);

    /// <summary>
    /// Changes a user's subscription plan and updates their credits accordingly
    /// </summary>
    Task<(bool Success, string Message)> ChangeSubscriptionAsync(int userId, string newPlan);

    /// <summary>
    /// Manually creates a new member from the admin panel
    /// </summary>
    Task<(bool Success, string Message)> CreateMemberAsync(AdminCreateMemberDto dto);
}