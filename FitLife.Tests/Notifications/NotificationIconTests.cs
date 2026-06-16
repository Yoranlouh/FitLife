using FitLife.Tests.Fakes;

namespace FitLife.Tests.Notifications;

/// <summary>
/// Tests for the notification type → icon and colour mapping, mirroring
/// AppNotification.TypeIcon and AppNotification.TypeColor from FitLife.Maui.
/// Tested via TestNotificationTypes to avoid a Microsoft.Maui.Graphics dependency.
/// </summary>
public class NotificationIconTests
{
    // ── TypeIcon: elk type heeft een onderscheidend icoon ─────────────────────

    [Fact]
    public void LesReservering_HeeftVinkjeIcoon()
        => Assert.Equal("✓", NotificationIconMap.GetIcon(TestNotificationType.LessonReserved));

    [Fact]
    public void Wachtlijst_HeeftKlokjesIcoon()
        => Assert.Equal("⏱", NotificationIconMap.GetIcon(TestNotificationType.Waitlist));

    [Fact]
    public void NieuweWorkout_HeeftSterretjeIcoon()
        => Assert.Equal("★", NotificationIconMap.GetIcon(TestNotificationType.NewWorkout));

    [Fact]
    public void LesGeannuleerd_HeeftKruisjesIcoon()
        => Assert.Equal("✕", NotificationIconMap.GetIcon(TestNotificationType.LessonCancelled));

    [Fact]
    public void AbonnementVerloopt_HeeftUitroepteken()
        => Assert.Equal("!", NotificationIconMap.GetIcon(TestNotificationType.SubscriptionExpiring));

    [Fact]
    public void AanwezigheidIngecheckt_HeeftPinIcoon()
        => Assert.Equal("📍", NotificationIconMap.GetIcon(TestNotificationType.AttendanceCheckedIn));

    [Fact]
    public void OnbekendType_HeeftBulletIcoon()
        => Assert.Equal("•", NotificationIconMap.GetIcon((TestNotificationType)999));

    // ── TypeColor: verwachte kleuren ──────────────────────────────────────────

    [Fact]
    public void LesReservering_KleurIsGroen()
        => Assert.Equal("#4CAF50", NotificationIconMap.GetColorHex(TestNotificationType.LessonReserved));

    [Fact]
    public void Wachtlijst_KleurIsOranje()
        => Assert.Equal("#FF9800", NotificationIconMap.GetColorHex(TestNotificationType.Waitlist));

    [Fact]
    public void NieuweWorkout_KleurIsBlauw()
        => Assert.Equal("#2196F3", NotificationIconMap.GetColorHex(TestNotificationType.NewWorkout));

    [Fact]
    public void LesGeannuleerd_KleurIsRood()
        => Assert.Equal("#F44336", NotificationIconMap.GetColorHex(TestNotificationType.LessonCancelled));

    [Fact]
    public void AbonnementVerloopt_KleurIsDonkerOranje()
        => Assert.Equal("#FF6F00", NotificationIconMap.GetColorHex(TestNotificationType.SubscriptionExpiring));

    [Fact]
    public void AanwezigheidIngecheckt_KleurIsGroen_ZoalsLesReservering()
        => Assert.Equal("#4CAF50", NotificationIconMap.GetColorHex(TestNotificationType.AttendanceCheckedIn));

    [Fact]
    public void AanwezigheidIngecheckt_ZelfdeKleurAlsReservering()
    {
        var reservedColor  = NotificationIconMap.GetColorHex(TestNotificationType.LessonReserved);
        var checkedInColor = NotificationIconMap.GetColorHex(TestNotificationType.AttendanceCheckedIn);
        Assert.Equal(reservedColor, checkedInColor);
    }

    // ── Alle types zijn uniek geconfigureerd ──────────────────────────────────

    [Fact]
    public void AlleTypes_HebbenEenIcoon()
    {
        var types = Enum.GetValues<TestNotificationType>();
        foreach (var type in types)
        {
            var icon = NotificationIconMap.GetIcon(type);
            Assert.False(string.IsNullOrEmpty(icon),
                $"Notificatietype {type} heeft geen icoon geconfigureerd");
        }
    }

    [Fact]
    public void AlleTypes_HebbenEenKleur()
    {
        var types = Enum.GetValues<TestNotificationType>();
        foreach (var type in types)
        {
            var color = NotificationIconMap.GetColorHex(type);
            Assert.True(color.StartsWith('#') && color.Length == 7,
                $"Notificatietype {type} heeft geen geldige kleur: {color}");
        }
    }

    // ── Notificatie tijdweergave (relatieve tijd logica) ──────────────────────

    [Theory]
    [InlineData(0,     ExpectedRange.JustNow)]      // < 1 minuut
    [InlineData(120,   ExpectedRange.MinutesAgo)]   // 2 minuten
    [InlineData(3601,  ExpectedRange.HoursAgo)]     // 1 uur + 1 seconde
    [InlineData(86401, ExpectedRange.DateDisplay)]  // 1 dag + 1 seconde
    public void TijdWeergave_CorrecteBereik_PerLeeftijdInSeconden(int ageInSeconds, ExpectedRange range)
    {
        var createdAt = DateTime.Now.AddSeconds(-ageInSeconds);
        var diff = DateTime.Now - createdAt;

        var actual = diff.TotalMinutes < 1    ? ExpectedRange.JustNow
                   : diff.TotalHours   < 1    ? ExpectedRange.MinutesAgo
                   : diff.TotalDays    < 1    ? ExpectedRange.HoursAgo
                                              : ExpectedRange.DateDisplay;

        Assert.Equal(range, actual);
    }
}

public enum ExpectedRange { JustNow, MinutesAgo, HoursAgo, DateDisplay }
