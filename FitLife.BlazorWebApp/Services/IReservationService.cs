using FitLife.BlazorWebApp.Models;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for reservation management
/// </summary>
public interface IReservationService
{
    Task<List<ReservationDto>> GetAllReservationsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ReservationDto>> GetReservationsByLessonAsync(int lessonId);
    Task<(bool Success, string Message)> CancelReservationAsync(int reservationId);
}