namespace API.Domain.Common;

// Helper methods for reusable date/time checks (e.g. weekday-based rules like Mon–Thu discounts)

public static class DateTimeHelper
{
    public static bool IsMaDiWoDo(DateTime date)
    {
        return date.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Thursday;
    }
    
    // dit is later CRUD te maken, voor nu nog even hardcoded '26-'27
    private static readonly List<DateTime> DutchHolidays = new()
    {
        // ===== 2026 =====
        new DateTime(2026, 1, 1),   // Nieuwjaarsdag
        new DateTime(2026, 4, 3),   // Goede Vrijdag
        new DateTime(2026, 4, 5),   // Eerste paasdag
        new DateTime(2026, 4, 6),   // Tweede paasdag
        new DateTime(2026, 4, 27),  // Koningsdag
        new DateTime(2026, 5, 5),   // Bevrijdingsdag
        new DateTime(2026, 5, 14),  // Hemelvaartsdag
        new DateTime(2026, 5, 24),  // Eerste pinksterdag
        new DateTime(2026, 5, 25),  // Tweede pinksterdag
        new DateTime(2026, 12, 25), // Eerste kerstdag
        new DateTime(2026, 12, 26), // Tweede kerstdag

        // ===== 2027 =====
        new DateTime(2027, 1, 1),   // Nieuwjaarsdag
        new DateTime(2027, 3, 26),  // Goede Vrijdag
        new DateTime(2027, 3, 28),  // Eerste paasdag
        new DateTime(2027, 3, 29),  // Tweede paasdag
        new DateTime(2027, 4, 27),  // Koningsdag
        new DateTime(2027, 5, 5),   // Bevrijdingsdag
        new DateTime(2027, 5, 6),   // Hemelvaartsdag
        new DateTime(2027, 5, 16),  // Eerste pinksterdag
        new DateTime(2027, 5, 17),  // Tweede pinksterdag
        new DateTime(2027, 12, 25), // Eerste kerstdag
        new DateTime(2027, 12, 26), // Tweede kerstdag
    };
    
    public static bool IsHoliday(DateTime date)
    {
        return DutchHolidays.Any(h => h.Date == date.Date);
    }
}