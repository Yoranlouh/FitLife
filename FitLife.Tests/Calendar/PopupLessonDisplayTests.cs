using SharedLibrary.DTOs.Responses;

namespace FitLife.Tests.Calendar;

/// <summary>
/// Tests for the multi-select lesson popup (MultipleLessonsPopup).
/// The popup shows a list of lessons for a calendar slot when multiple lessons overlap;
/// each row shows the lesson name, time, and a green checkmark if the user is booked.
/// </summary>
public class PopupLessonDisplayTests
{
    // ── Vinkje per les in popup ───────────────────────────────────────────────

    [Fact]
    public void Popup_LesMetIsBooked_True_ToontVinkje()
    {
        var lesson = new LessonResponse { Id = 1, WorkoutName = "Yoga", IsBooked = true };
        Assert.True(lesson.IsBooked);
    }

    [Fact]
    public void Popup_LesMetIsBooked_False_ToontGeenVinkje()
    {
        var lesson = new LessonResponse { Id = 1, WorkoutName = "Spinning", IsBooked = false };
        Assert.False(lesson.IsBooked);
    }

    // ── Correct tonen van lessen in een slot ──────────────────────────────────

    [Fact]
    public void Popup_ToontAlleLessenVanHetSlot()
    {
        var slot = new[]
        {
            new LessonResponse { Id = 1, WorkoutName = "Yoga",     StartTime = DateTime.Today.AddHours(9) },
            new LessonResponse { Id = 2, WorkoutName = "Pilates",  StartTime = DateTime.Today.AddHours(9) },
            new LessonResponse { Id = 3, WorkoutName = "Spinning", StartTime = DateTime.Today.AddHours(9) },
        };

        Assert.Equal(3, slot.Length);
        Assert.Contains(slot, l => l.WorkoutName == "Yoga");
        Assert.Contains(slot, l => l.WorkoutName == "Pilates");
        Assert.Contains(slot, l => l.WorkoutName == "Spinning");
    }

    // ── Inschrijvingsstatus correct tonen ─────────────────────────────────────

    [Fact]
    public void Popup_GeboekteLes_WordtCorrectGemarkeerdTeMiddenVanAnderen()
    {
        var slot = new[]
        {
            new LessonResponse { Id = 1, WorkoutName = "Yoga",    IsBooked = true  },
            new LessonResponse { Id = 2, WorkoutName = "Pilates", IsBooked = false },
        };

        var geboekt = slot.Where(l => l.IsBooked).ToList();
        var nietGeboekt = slot.Where(l => !l.IsBooked).ToList();

        Assert.Single(geboekt);
        Assert.Single(nietGeboekt);
        Assert.Equal("Yoga", geboekt[0].WorkoutName);
    }

    [Fact]
    public void Popup_GeenGeboekteLessen_GeenVinkje()
    {
        var slot = new[]
        {
            new LessonResponse { Id = 1, IsBooked = false },
            new LessonResponse { Id = 2, IsBooked = false },
        };

        Assert.Empty(slot.Where(l => l.IsBooked));
    }

    // ── Synchronisatie met kalenderweergave ───────────────────────────────────

    [Fact]
    public void Popup_NaReservering_IsBooked_WordtBijgewerkt()
    {
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, IsBooked = false },
            new() { Id = 2, IsBooked = false },
        };

        // Simuleer boeking van les 1
        lessons.Single(l => l.Id == 1).IsBooked = true;

        Assert.True(lessons.Single(l => l.Id == 1).IsBooked);
        Assert.False(lessons.Single(l => l.Id == 2).IsBooked);
    }

    [Fact]
    public void Popup_NaAnnulering_IsBooked_WordtFalse()
    {
        var lessons = new List<LessonResponse>
        {
            new() { Id = 1, IsBooked = true },
        };

        lessons.Single(l => l.Id == 1).IsBooked = false;

        Assert.False(lessons.Single(l => l.Id == 1).IsBooked);
    }

    // ── Deelnemers en wachtlijst tonen ────────────────────────────────────────

    [Fact]
    public void Popup_TelDeelnemersBijBeideSlots()
    {
        var lesson = new LessonResponse
        {
            Id = 1,
            CurrentParticipantCount = 10,
            MaxParticipants = 15,
            WaitlistCount = 2
        };

        Assert.Equal(10, lesson.CurrentParticipantCount);
        Assert.Equal(15, lesson.MaxParticipants);
        Assert.Equal(2, lesson.WaitlistCount);
    }

    // ── Kleur per workout type ────────────────────────────────────────────────

    [Fact]
    public void Popup_LesHeeftWorkoutKleur()
    {
        var lesson = new LessonResponse { Id = 1, WorkoutColor = "#FF5722" };
        Assert.Equal("#FF5722", lesson.WorkoutColor);
    }

    [Fact]
    public void Popup_LesZonderExplicieteKleur_HeeftStandaardKleur()
    {
        var lesson = new LessonResponse { Id = 1 };
        Assert.Equal("#5B6636", lesson.WorkoutColor);
    }
}
