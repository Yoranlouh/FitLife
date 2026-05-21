namespace FitLife.BlazorWebApp.Models;

/// <summary>
/// DTO for user authentication and session management
/// </summary>
public class UserSessionDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "admin", "instructor"
    public bool IsAuthenticated { get; set; }
}

/// <summary>
/// DTO for displaying lessons in the admin panel
/// </summary>
public class LessonDto
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public int WaitlistCount { get; set; }
    
    public int WorkoutId { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    
    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    
    // Calculated properties
    public bool IsFull => CurrentParticipants >= MaxParticipants;
    public int AvailableSpots => Math.Max(0, MaxParticipants - CurrentParticipants);
    public double OccupancyPercentage => MaxParticipants > 0 
        ? Math.Round((double)CurrentParticipants / MaxParticipants * 100, 1) 
        : 0;
}

/// <summary>
/// DTO for creating or editing a lesson
/// </summary>
public class LessonEditDto
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Today.AddHours(9);
    public int DurationMinutes { get; set; } = 60;
    public int? CapacityOverride { get; set; }
    public int WorkoutId { get; set; }
    public int InstructorId { get; set; }
    public int LocationId { get; set; }
    
    // Recurrence settings
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; } // "Daily", "Weekly", "Monthly"
    public int? RecurrenceInterval { get; set; } = 1;
    public DateTime? RecurrenceEndDate { get; set; }
    public int? RecurrenceCount { get; set; }
}

/// <summary>
/// DTO for instructor information
/// </summary>
public class InstructorDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; set; }
    public string? Specialization { get; set; }
    public string? PhotoUrl { get; set; }
    public int TotalLessons { get; set; }
    public int UpcomingLessons { get; set; }
}

/// <summary>
/// DTO for workout types
/// </summary>
public class WorkoutDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int DefaultCapacity { get; set; }
    public int TotalLessons { get; set; }
}

/// <summary>
/// DTO for locations
/// </summary>
public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int Capacity { get; set; }
    public int TotalLessons { get; set; }
}

/// <summary>
/// DTO for reservations in admin view
/// </summary>
public class ReservationDto
{
    public int Id { get; set; }
    public DateTime ReservationDate { get; set; }
    public bool IsCancelled { get; set; }
    
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberEmail { get; set; }
    
    public int LessonId { get; set; }
    public string LessonInfo { get; set; } = string.Empty;
    public DateTime LessonStartTime { get; set; }
}

/// <summary>
/// DTO for dashboard statistics
/// </summary>
public class DashboardStatsDto
{
    public int TotalMembers { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalLessonsThisWeek { get; set; }
    public int TotalReservationsThisWeek { get; set; }
    public double AverageOccupancyPercentage { get; set; }
    public int UpcomingLessonsToday { get; set; }
    public List<LessonDto> TodaysLessons { get; set; } = new();
    public List<PopularWorkoutDto> PopularWorkouts { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

/// <summary>
/// DTO for popular workout statistics
/// </summary>
public class PopularWorkoutDto
{
    public string WorkoutName { get; set; } = string.Empty;
    public int TotalReservations { get; set; }
    public double AverageOccupancy { get; set; }
}

/// <summary>
/// DTO for recent activity feed
/// </summary>
public class RecentActivityDto
{
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty; // "reservation", "cancellation", "new_lesson"
}

/// <summary>
/// DTO for member/user management
/// </summary>
public class MemberDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int TotalReservations { get; set; }
    public DateTime? LastActivity { get; set; }
}