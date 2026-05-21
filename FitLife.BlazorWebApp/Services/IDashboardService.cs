using FitLife.BlazorWebApp.Models;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for dashboard statistics
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves comprehensive dashboard statistics for admin overview
    /// </summary>
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}