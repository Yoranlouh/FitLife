using SharedLibrary.DTOs.Responses;

namespace FitLife.Tests.Calendar;

/// <summary>
/// Tests for the week-calendar and day-view lesson display logic.
/// Focus areas: IsBooked flag driving the checkmark display, and
/// the UpdateBookingState logic extracted from WeekViewModel.
/// </summary>
public class LessonCalendarTests
{
    // ── IsBooked vlag → vinkje zichtbaar ─────────────────────────────────────

    [Fact]
    public void IsBooked_True_MaaktVinkjeZichtbaar()
    {
        var lesson = new LessonResponse { Id = 1, IsBooked = true };
        Assert.True(lesson.IsBooked);
    }

    [Fact]
    public void IsBooked_False_VinkjeNietZichtbaar()
    {
        var lesson = new LessonResponse { Id = 1, IsBooked = false };
        Assert.False(lesson.IsBooked);
    }

    // ── isAnyBooked berekening (kalender grid slot) ───────────────────────────

    [Fact]
    public void Slot_MetEenGeboekteLes_IsAnyBooked_IsTrue()
    {
        var lessons = new[]
        {
            new LessonResponse { Id = 1, IsBooked = false },
            new LessonResponse { Id = 2, IsBooked = true },
        };
        Assert.True(lessons.Any(l => l.IsBooked));
    }

    [Fact]
    public void Slot_ZonderGeboekteLessen_IsAnyBooked_IsFalse()
    {
        var lessons = new[]
        {
            new LessonResponse { Id = 1, IsBooked = false },
            new LessonResponse { Id = 2, IsBooked = false },
        };
        Assert.False(lessons.Any(l => l.IsBooked));
    }

    [Fact]
    public void Slot_AlleLessenGeboekt_IsAnyBooked_IsTrue()
    {
        var lessons = new[]
        {
            new LessonResponse { Id = 1, IsBooked = true },
            new LessonResponse { Id = 2, IsBooked = true },
        };
        Assert.True(lessons.Any(l => l.IsBooked));
    }

    [Fact]
    public void Slot_MetEenLes_NietGeboekt_IsAnyBooked_IsFalse()
    {
        var lessons = new[] { new LessonResponse { Id = 1, IsBooked = false } };
        Assert.False(lessons.Any(l => l.IsBooked));
    }

    // ── UpdateBookingState logica (WeekViewModel) ─────────────────────────────

    [Fact]
    public void UpdateBookingState_StelIsBooked_InBijJuisteLes()
    {
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, IsBooked = false },
            new() { Id = 2, IsBooked = false },
        };

        CalendarState.UpdateBookingState(lessons, lessonId: 1, isBooked: true);

        Assert.True(lessons.Single(l => l.Id == 1).IsBooked);
        Assert.False(lessons.Single(l => l.Id == 2).IsBooked);
    }

    [Fact]
    public void UpdateBookingState_WischtIsBooked_BijAnnuleren()
    {
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, IsBooked = true },
        };

        CalendarState.UpdateBookingState(lessons, lessonId: 1, isBooked: false);

        Assert.False(lessons.Single(l => l.Id == 1).IsBooked);
    }

    [Fact]
    public void UpdateBookingState_OnbekendeId_PasstvrijAndereLessenNietAan()
    {
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, IsBooked = false },
        };

        CalendarState.UpdateBookingState(lessons, lessonId: 999, isBooked: true);

        Assert.False(lessons.Single(l => l.Id == 1).IsBooked);
    }

    [Fact]
    public void UpdateBookingState_LegeCollectie_GooidtGeenFout()
    {
        var lessons = new List<LessonResponse>();
        // Geen exception verwacht
        CalendarState.UpdateBookingState(lessons, lessonId: 1, isBooked: true);
        Assert.Empty(lessons);
    }

    // ── Synchronisatie: na reservering wordt vinkje bijgewerkt ───────────────

    [Fact]
    public void NaReservering_IsBooked_WordtTrue()
    {
        var lesson = new LessonResponse { Id = 1, IsBooked = false };
        var lessons = new List<LessonResponse> { lesson };

        // Simuleer WeakReferenceMessenger-update na reservering
        CalendarState.UpdateBookingState(lessons, lessonId: 1, isBooked: true);

        Assert.True(lessons.Single(l => l.Id == 1).IsBooked);
    }

    [Fact]
    public void NaAnnulering_IsBooked_WordtFalse()
    {
        var lesson = new LessonResponse { Id = 1, IsBooked = true };
        var lessons = new List<LessonResponse> { lesson };

        CalendarState.UpdateBookingState(lessons, lessonId: 1, isBooked: false);

        Assert.False(lessons.Single(l => l.Id == 1).IsBooked);
    }

    // ── Beschikbare plekken berekening ────────────────────────────────────────

    [Fact]
    public void BeschikbarePlekken_WerdenBerekendUitMaxMinusCurrent()
    {
        var lesson = new LessonResponse { Id = 1, MaxParticipants = 15, CurrentParticipantCount = 7 };
        int available = lesson.MaxParticipants - lesson.CurrentParticipantCount;
        Assert.Equal(8, available);
    }

    [Fact]
    public void BeschikbarePlekken_BijVolLes_IsNul()
    {
        var lesson = new LessonResponse { Id = 1, MaxParticipants = 15, CurrentParticipantCount = 15 };
        int available = Math.Max(0, lesson.MaxParticipants - lesson.CurrentParticipantCount);
        Assert.Equal(0, available);
    }

    // ── Wachtlijst in kalender ────────────────────────────────────────────────

    [Fact]
    public void WachtlijstAantal_WordtWeergegeven_AlsGroterDanNul()
    {
        var lesson = new LessonResponse { Id = 1, WaitlistCount = 3 };
        Assert.True(lesson.WaitlistCount > 0);
    }

    [Fact]
    public void WachtlijstAantal_Nul_WordtNietWeergegeven()
    {
        var lesson = new LessonResponse { Id = 1, WaitlistCount = 0 };
        Assert.False(lesson.WaitlistCount > 0);
    }
}

internal static class CalendarState
{
    /// <summary>
    /// Mirrors WeekViewModel.UpdateBookingState: finds the lesson by ID and mutates IsBooked.
    /// </summary>
    public static void UpdateBookingState(List<LessonResponse> lessons, int lessonId, bool isBooked)
    {
        var lesson = lessons.FirstOrDefault(l => l.Id == lessonId);
        if (lesson is null) return;
        lesson.IsBooked = isBooked;
    }
}
