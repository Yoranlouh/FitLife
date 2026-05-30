using FitLife.BlazorWebApp.Models;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for dashboard statistics
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<InstructorDashboardDto> GetInstructorDashboardAsync(int userId);
}