namespace FitLife.Tests.Lessons;

// Tests for the booking window, cancellation deadline and check-in window rules
// extracted from LessonDetailViewModel.ApplyQueryAttributes.
// All tests use a fixed "now" so time-dependent logic is deterministic.
//
// The rules span three user stories, so the tests are split into one [Trait]-tagged
// class per story. The pure-C# replica of the rules lives in BookingRules at the
// bottom of this file and is shared by all three classes.
//
//   • US-L02 — Les reserveren          (boekvenster, reserveerknop, verlopen-melding)
//   • US-L03 — Reservering annuleren    (annuleringsdeadline)
//   • US-I03 — Presentie registreren    (check-in venster, check-in knoppen/meldingen)

internal static class BookingRulesTestTime
{
    public static readonly DateTime Now = new(2026, 6, 15, 10, 0, 0);
}

/// <summary>
/// US-L02 — Les reserveren (Lid, Must have).
/// "Reserveren is niet mogelijk wanneer de les vol is of al begonnen is" en
/// het boekvenster (max. 7 dagen vooruit) bepalen of de reserveerknop zichtbaar is.
/// </summary>
[Trait("UserStory", "US-L02")]
[Trait("Rol", "Lid")]
public class ReserveWindowRulesTests
{
    private static readonly DateTime Now = BookingRulesTestTime.Now;

    // ── IsTooFarInFuture: 7-dagenvenster ─────────────────────────────────────

    [Fact]
    public void TooFarInFuture_Les8DagenVooruit_IsTrue()
    {
        var start = Now.AddDays(8);
        Assert.True(BookingRules.IsTooFarInFuture(start, Now));
    }

    [Fact]
    public void TooFarInFuture_LesExact7DagenVooruit_IsFalse()
    {
        // Boundary: start = now + 7 days → NOT too far (exactly at the limit)
        var start = Now.AddDays(7);
        Assert.False(BookingRules.IsTooFarInFuture(start, Now));
    }

    [Fact]
    public void TooFarInFuture_Les6DagenVooruit_IsFalse()
    {
        var start = Now.AddDays(6);
        Assert.False(BookingRules.IsTooFarInFuture(start, Now));
    }

    [Fact]
    public void TooFarInFuture_LesVandaag_IsFalse()
    {
        Assert.False(BookingRules.IsTooFarInFuture(Now.AddHours(2), Now));
    }

    // ── IsLessonStartedOrPast ─────────────────────────────────────────────────

    [Fact]
    public void LessonStartedOrPast_LesNetBegonnen_IsTrue()
    {
        var start = Now;
        Assert.True(BookingRules.IsLessonStartedOrPast(start, Now));
    }

    [Fact]
    public void LessonStartedOrPast_LesInVerleden_IsTrue()
    {
        var start = Now.AddHours(-1);
        Assert.True(BookingRules.IsLessonStartedOrPast(start, Now));
    }

    [Fact]
    public void LessonStartedOrPast_LesInToekomst_IsFalse()
    {
        var start = Now.AddMinutes(5);
        Assert.False(BookingRules.IsLessonStartedOrPast(start, Now));
    }

    // ── ShowReserveButton ────────────────────────────────────────────────────

    [Fact]
    public void ShowReserveButton_AlleVoorwaardenOk_IsTrue()
    {
        Assert.True(BookingRules.ShowReserveButton(
            isReserved: false, isTooFarInFuture: false, isLessonStartedOrPast: false));
    }

    [Fact]
    public void ShowReserveButton_AlIngeschreven_IsFalse()
    {
        Assert.False(BookingRules.ShowReserveButton(
            isReserved: true, isTooFarInFuture: false, isLessonStartedOrPast: false));
    }

    [Fact]
    public void ShowReserveButton_TooFarInFuture_IsFalse()
    {
        Assert.False(BookingRules.ShowReserveButton(
            isReserved: false, isTooFarInFuture: true, isLessonStartedOrPast: false));
    }

    [Fact]
    public void ShowReserveButton_LesAlGestart_IsFalse()
    {
        Assert.False(BookingRules.ShowReserveButton(
            isReserved: false, isTooFarInFuture: false, isLessonStartedOrPast: true));
    }

    // ── ShowExpiredMessage ────────────────────────────────────────────────────

    [Fact]
    public void ShowExpiredMessage_LesVerlopenEnNietIngeschreven_IsTrue()
        => Assert.True(BookingRules.ShowExpiredMessage(isReserved: false, isLessonStartedOrPast: true));

    [Fact]
    public void ShowExpiredMessage_IngeschrevenVoorVerlopen_IsFalse()
        => Assert.False(BookingRules.ShowExpiredMessage(isReserved: true, isLessonStartedOrPast: true));
}

/// <summary>
/// US-L03 — Reservering annuleren (Lid, Must have).
/// "Annuleren kan tot een ingestelde termijn vóór aanvang van de les."
/// De grens is 1 uur vóór aanvang; daarna is de annuleringsdeadline verstreken.
/// </summary>
[Trait("UserStory", "US-L03")]
[Trait("Rol", "Lid")]
public class CancellationDeadlineRulesTests
{
    private static readonly DateTime Now = BookingRulesTestTime.Now;

    // ── IsCancellationDeadlinePassed: 1-uurgrens ─────────────────────────────

    [Fact]
    public void AnnulerenDeadline_LesStart30MinutenVooruit_IsTrue()
    {
        var start = Now.AddMinutes(30);
        Assert.True(BookingRules.IsCancellationDeadlinePassed(start, Now));
    }

    [Fact]
    public void AnnulerenDeadline_LesStartExact1UurVooruit_IsTrue()
    {
        // Boundary: ≤ 1 hour before → deadline passed
        var start = Now.AddHours(1);
        Assert.True(BookingRules.IsCancellationDeadlinePassed(start, Now));
    }

    [Fact]
    public void AnnulerenDeadline_LesStart90MinutenVooruit_IsFalse()
    {
        var start = Now.AddMinutes(90);
        Assert.False(BookingRules.IsCancellationDeadlinePassed(start, Now));
    }

    [Fact]
    public void AnnulerenDeadline_LesInVerleden_IsTrue()
    {
        var start = Now.AddHours(-2);
        Assert.True(BookingRules.IsCancellationDeadlinePassed(start, Now));
    }

    // ── ShowCancellationDeadlineWarning ───────────────────────────────────────

    [Fact]
    public void ShowCancellationWarning_IngeschrevenEnDeadlineVerlopen_IsTrue()
        => Assert.True(BookingRules.ShowCancellationDeadlineWarning(
               isReserved: true, isCancellationDeadlinePassed: true));

    [Fact]
    public void ShowCancellationWarning_NietIngeschreven_IsFalse()
        => Assert.False(BookingRules.ShowCancellationDeadlineWarning(
               isReserved: false, isCancellationDeadlinePassed: true));

    [Fact]
    public void ShowCancellationWarning_DeadlineNietVerlopen_IsFalse()
        => Assert.False(BookingRules.ShowCancellationDeadlineWarning(
               isReserved: true, isCancellationDeadlinePassed: false));
}

/// <summary>
/// US-I03 — Presentie registreren (Instructeur, Could have).
/// "Presentie kan alleen geregistreerd worden voor lessen die bezig of afgelopen zijn."
/// Het check-in venster opent 15 minuten vóór aanvang en sluit aan het einde van de les;
/// de zichtbaarheid van de check-in knoppen/meldingen volgt daaruit.
/// </summary>
[Trait("UserStory", "US-I03")]
[Trait("Rol", "Instructeur")]
public class CheckInWindowRulesTests
{
    private static readonly DateTime Now = BookingRulesTestTime.Now;

    // ── Check-in venster: 15 minuten voor aanvang tot einde les ──────────────

    [Fact]
    public void CheckInVenster_15MinutenVoorAanvang_IsOpen()
    {
        var start = Now.AddMinutes(15);
        var end   = Now.AddMinutes(75);
        Assert.True(BookingRules.IsCheckInWindowOpen(start, end, Now));
    }

    [Fact]
    public void CheckInVenster_20MinutenVoorAanvang_IsNogNietOpen()
    {
        // Check-in opent op start - 15 min; 20 min voor start → nog gesloten (5 min te vroeg)
        var start = Now.AddMinutes(20);
        var end   = Now.AddMinutes(80);
        Assert.False(BookingRules.IsCheckInWindowOpen(start, end, Now));
    }

    [Fact]
    public void CheckInVenster_TijdensLes_IsOpen()
    {
        var start = Now.AddMinutes(-20);  // les al 20 min bezig
        var end   = Now.AddMinutes(40);
        Assert.True(BookingRules.IsCheckInWindowOpen(start, end, Now));
    }

    [Fact]
    public void CheckInVenster_NaAfloopLes_IsGesloten()
    {
        var start = Now.AddHours(-2);
        var end   = Now.AddHours(-1);
        Assert.False(BookingRules.IsCheckInWindowOpen(start, end, Now));
    }

    [Fact]
    public void CheckInVenster_ExactBijEinde_IsGesloten()
    {
        // Boundary: now == end → window closed (strict <)
        var start = Now.AddMinutes(-60);
        var end   = Now;   // einde is nu
        Assert.False(BookingRules.IsCheckInWindowOpen(start, end, Now));
    }

    // ── ShowCheckInSection / ShowCheckedInBadge ───────────────────────────────

    [Fact]
    public void ShowCheckInSection_IngeschrevenGebruiker_IsTrue()
        => Assert.True(BookingRules.ShowCheckInSection(isReserved: true));

    [Fact]
    public void ShowCheckInSection_NietIngeschrevenGebruiker_IsFalse()
        => Assert.False(BookingRules.ShowCheckInSection(isReserved: false));

    [Fact]
    public void ShowCheckedInBadge_NaCheckIn_IsTrue()
        => Assert.True(BookingRules.ShowCheckedInBadge(isCheckedIn: true));

    [Fact]
    public void ShowCheckedInBadge_VoorCheckIn_IsFalse()
        => Assert.False(BookingRules.ShowCheckedInBadge(isCheckedIn: false));

    // ── ShowCheckInButtons ────────────────────────────────────────────────────

    [Fact]
    public void ShowCheckInButtons_IngeschrevenEnVensterOpen_NogNietIngecheckt_IsTrue()
        => Assert.True(BookingRules.ShowCheckInButtons(
               isReserved: true, isCheckedIn: false, isWindowOpen: true));

    [Fact]
    public void ShowCheckInButtons_AlIngecheckt_IsFalse()
        => Assert.False(BookingRules.ShowCheckInButtons(
               isReserved: true, isCheckedIn: true, isWindowOpen: true));

    [Fact]
    public void ShowCheckInButtons_VensterGesloten_IsFalse()
        => Assert.False(BookingRules.ShowCheckInButtons(
               isReserved: true, isCheckedIn: false, isWindowOpen: false));

    [Fact]
    public void ShowCheckInButtons_NietIngeschreven_IsFalse()
        => Assert.False(BookingRules.ShowCheckInButtons(
               isReserved: false, isCheckedIn: false, isWindowOpen: true));

    // ── ShowCheckInEarlyMessage / ShowLessonEndedMessage ─────────────────────

    [Fact]
    public void ShowCheckInEarlyMessage_TeVroeg_IsTrue()
        => Assert.True(BookingRules.ShowCheckInEarlyMessage(
               isReserved: true, isCheckedIn: false, isWindowOpen: false, isLessonEnded: false));

    [Fact]
    public void ShowCheckInEarlyMessage_LesVoorbij_IsFalse()
        => Assert.False(BookingRules.ShowCheckInEarlyMessage(
               isReserved: true, isCheckedIn: false, isWindowOpen: false, isLessonEnded: true));

    [Fact]
    public void ShowLessonEndedMessage_NaAfloop_NietIngecheckt_IsTrue()
        => Assert.True(BookingRules.ShowLessonEndedMessage(
               isReserved: true, isCheckedIn: false, isLessonEnded: true));

    [Fact]
    public void ShowLessonEndedMessage_NaAfloop_WelIngecheckt_IsFalse()
        => Assert.False(BookingRules.ShowLessonEndedMessage(
               isReserved: true, isCheckedIn: true, isLessonEnded: true));
}

/// <summary>
/// Pure-C# replica of the booking-rule computed properties from LessonDetailViewModel.
/// Extracted for testability without MAUI dependencies.
/// </summary>
internal static class BookingRules
{
    public static bool IsTooFarInFuture(DateTime lessonStart, DateTime now)
        => lessonStart > now.AddDays(7);

    public static bool IsLessonStartedOrPast(DateTime lessonStart, DateTime now)
        => lessonStart <= now;

    public static bool IsCancellationDeadlinePassed(DateTime lessonStart, DateTime now)
        => lessonStart <= now.AddHours(1);

    public static bool IsCheckInWindowOpen(DateTime lessonStart, DateTime lessonEnd, DateTime now)
        => now >= lessonStart.AddMinutes(-15) && now < lessonEnd;

    public static bool ShowReserveButton(bool isReserved, bool isTooFarInFuture, bool isLessonStartedOrPast)
        => !isReserved && !isTooFarInFuture && !isLessonStartedOrPast;

    public static bool ShowCheckInSection(bool isReserved)
        => isReserved;

    public static bool ShowCheckedInBadge(bool isCheckedIn)
        => isCheckedIn;

    public static bool ShowCheckInButtons(bool isReserved, bool isCheckedIn, bool isWindowOpen)
        => isReserved && !isCheckedIn && isWindowOpen;

    public static bool ShowGpsCheckInButton(bool isReserved, bool isCheckedIn, bool isWindowOpen, bool locationEnabled)
        => isReserved && !isCheckedIn && isWindowOpen && locationEnabled;

    public static bool ShowCheckInEarlyMessage(bool isReserved, bool isCheckedIn, bool isWindowOpen, bool isLessonEnded)
        => isReserved && !isCheckedIn && !isWindowOpen && !isLessonEnded;

    public static bool ShowLessonEndedMessage(bool isReserved, bool isCheckedIn, bool isLessonEnded)
        => isReserved && !isCheckedIn && isLessonEnded;

    public static bool ShowExpiredMessage(bool isReserved, bool isLessonStartedOrPast)
        => !isReserved && isLessonStartedOrPast;

    public static bool ShowCancellationDeadlineWarning(bool isReserved, bool isCancellationDeadlinePassed)
        => isReserved && isCancellationDeadlinePassed;
}
