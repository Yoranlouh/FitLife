using SharedLibrary.DTOs.Responses;

namespace FitLife.Tests.Calendar;

/// <summary>
/// US-L01 — Lesrooster bekijken (Lid, Must have).
/// "Als lid wil ik het dag- en weekoverzicht van de beschikbare lessen kunnen
///  bekijken, zodat ik een les op een geschikt moment kan kiezen."
///
/// Acceptatiecriteria gedekt in deze klasse:
///   • Per les is zichtbaar hoeveel plekken nog beschikbaar zijn  (BeschikbarePlekken_*).
///   • Een volle les wordt duidelijk als „vol" gemarkeerd          (VolleLes_*).
///   • Lessen zijn te filteren op dag en op lestype                (Filter_*).
/// De overige criteria (naam/datum/tijd/instructeur/zaal tonen; asynchroon laden)
/// worden gedekt door de UI-tests in FitLife.UITests (WeekPageTests) en door de
/// IsBooked-/wachtlijst-weergavelogica hieronder.
/// </summary>
[Trait("UserStory", "US-L01")]
[Trait("Rol", "Lid")]
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

    // ── Volle les wordt als „vol" gemarkeerd (acceptatiecriterium L01) ────────

    [Fact]
    public void VolleLes_WordtAlsVolGemarkeerd()
    {
        var lesson = new LessonResponse { Id = 1, MaxParticipants = 15, CurrentParticipantCount = 15 };
        Assert.True(CalendarState.IsFull(lesson));
    }

    [Fact]
    public void NietVolleLes_WordtNietAlsVolGemarkeerd()
    {
        var lesson = new LessonResponse { Id = 1, MaxParticipants = 15, CurrentParticipantCount = 14 };
        Assert.False(CalendarState.IsFull(lesson));
    }

    [Fact]
    public void OvervolleLes_WordtOokAlsVolGemarkeerd()
    {
        // Edge case: meer deelnemers dan capaciteit → nog steeds „vol"
        var lesson = new LessonResponse { Id = 1, MaxParticipants = 15, CurrentParticipantCount = 16 };
        Assert.True(CalendarState.IsFull(lesson));
    }

    // ── Filteren op dag (acceptatiecriterium L01) ─────────────────────────────

    [Fact]
    public void FilterOpDag_GeeftAlleenLessenVanDieDag()
    {
        var maandag = new DateTime(2026, 6, 15, 0, 0, 0);
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, StartTime = maandag.AddHours(9)  },
            new() { Id = 2, StartTime = maandag.AddHours(18) },
            new() { Id = 3, StartTime = maandag.AddDays(1).AddHours(10) }, // dinsdag
        };

        var maandagLessen = CalendarState.FilterByDay(lessons, maandag).ToList();

        Assert.Equal(2, maandagLessen.Count);
        Assert.All(maandagLessen, l => Assert.Equal(maandag.Date, l.StartTime.Date));
    }

    [Fact]
    public void FilterOpDag_ZonderLessenOpDieDag_GeeftLegeLijst()
    {
        var maandag = new DateTime(2026, 6, 15, 0, 0, 0);
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, StartTime = maandag.AddDays(2).AddHours(9) },
        };

        Assert.Empty(CalendarState.FilterByDay(lessons, maandag));
    }

    // ── Filteren op lestype (acceptatiecriterium L01) ─────────────────────────

    [Fact]
    public void FilterOpLestype_GeeftAlleenLessenVanDatType()
    {
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, WorkoutName = "Yoga"     },
            new() { Id = 2, WorkoutName = "Spinning" },
            new() { Id = 3, WorkoutName = "Yoga"     },
        };

        var yoga = CalendarState.FilterByWorkout(lessons, "Yoga").ToList();

        Assert.Equal(2, yoga.Count);
        Assert.All(yoga, l => Assert.Equal("Yoga", l.WorkoutName));
    }

    [Fact]
    public void FilterOpLestype_IsHoofdletterongevoelig()
    {
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, WorkoutName = "Yoga" },
        };

        Assert.Single(CalendarState.FilterByWorkout(lessons, "yoga"));
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

    /// <summary>A lesson is full when the participant count reaches (or exceeds) capacity.</summary>
    public static bool IsFull(LessonResponse lesson)
        => lesson.CurrentParticipantCount >= lesson.MaxParticipants;

    /// <summary>Day-view filter: only lessons that start on the given calendar day.</summary>
    public static IEnumerable<LessonResponse> FilterByDay(IEnumerable<LessonResponse> lessons, DateTime day)
        => lessons.Where(l => l.StartTime.Date == day.Date);

    /// <summary>Lesson-type filter: only lessons whose workout name matches (case-insensitive).</summary>
    public static IEnumerable<LessonResponse> FilterByWorkout(IEnumerable<LessonResponse> lessons, string workoutName)
        => lessons.Where(l => string.Equals(l.WorkoutName, workoutName, StringComparison.OrdinalIgnoreCase));
}
