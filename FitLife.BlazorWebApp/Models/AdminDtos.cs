using System.ComponentModel.DataAnnotations;

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
    // Effective capacity: COALESCE(capacity_override, workout.default_capacity, location.capacity)
    public int MaxParticipants { get; set; }
    // Raw DB value — null means the lesson inherits capacity from the location/workout
    public int? CapacityOverride { get; set; }
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

    [Required(ErrorMessage = "Startdatum en -tijd zijn verplicht")]
    public DateTime StartTime { get; set; } = DateTime.Today.AddHours(9);

    [Range(15, 480, ErrorMessage = "Duur moet tussen 15 en 480 minuten zijn")]
    public int DurationMinutes { get; set; } = 60;

    [Range(1, 1000, ErrorMessage = "Max deelnemers moet tussen 1 en 1000 zijn")]
    public int? CapacityOverride { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecteer een workout")]
    public int WorkoutId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecteer een instructeur")]
    public int InstructorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecteer een locatie")]
    public int LocationId { get; set; }
}

/// <summary>
/// DTO for instructor information
/// </summary>
public class InstructorDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Voornaam is verplicht")]
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
    public string FullName => string.IsNullOrEmpty(LastName) ? FirstName : $"{FirstName} {LastName}";
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

    [Required(ErrorMessage = "Naam is verplicht")]
    [StringLength(100, ErrorMessage = "Naam mag maximaal 100 tekens zijn")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 1000, ErrorMessage = "Capaciteit moet tussen 1 en 1000 zijn")]
    public int DefaultCapacity { get; set; }

    public string Color { get; set; } = "#5B6636";

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
/// DTO for instructor dashboard statistics
/// </summary>
public class InstructorDashboardDto
{
    public int TotalLessonsGiven { get; set; }
    public int TotalUpcomingLessons { get; set; }
    public int TotalStudentsTaught { get; set; }
    public List<LessonDto> TodaysLessons { get; set; } = new();
    public List<LessonDto> RecentLessons { get; set; } = new();
    public List<LessonDto> UpcomingLessons { get; set; } = new();
    public List<InstructorWorkoutStatDto> TopWorkouts { get; set; } = new();
    public List<LessonDto> ParticipatedLessons { get; set; } = new();
}

/// <summary>
/// Workout statistics for instructor dashboard
/// </summary>
public class InstructorWorkoutStatDto
{
    public string WorkoutName { get; set; } = string.Empty;
    public int LessonCount { get; set; }
    public int TotalStudents { get; set; }
}

/// <summary>
/// DTO for manually creating a new member from the admin panel
/// </summary>
public class AdminCreateMemberDto
{
    [Required(ErrorMessage = "Voornaam is verplicht")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Achternaam is verplicht")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "E-mailadres is verplicht")]
    [EmailAddress(ErrorMessage = "Ongeldig e-mailadres")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Wachtwoord is verplicht")]
    [MinLength(8, ErrorMessage = "Wachtwoord moet minimaal 8 tekens zijn")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Herhaal wachtwoord")]
    public string ConfirmPassword { get; set; } = "";

    [Required]
    public string SubscriptionPlan { get; set; } = "Rookie";

    public bool IsYearly { get; set; } = false;

    [Required(ErrorMessage = "Kies een startdatum")]
    public DateTime StartDate { get; set; } = DateTime.Today;
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
    public string? SubscriptionType { get; set; }
    public DateTime? SubscriptionRenewalDate { get; set; }
    public bool SubscriptionPaused { get; set; }
}