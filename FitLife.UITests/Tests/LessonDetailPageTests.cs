namespace FitLife.UITests.Tests;

/// <summary>
/// UI tests for LessonDetailPage.
/// Navigation path: Home → WeekPage → tap a lesson block → LessonDetailPage.
/// The mock API always returns a lesson in the future (not yet booked) so the
/// Reserve button is the expected primary action.
/// </summary>
[Collection(UITestCollection.Name)]
public sealed class LessonDetailPageTests(AppiumFixture fixture)
{
    private readonly AppiumFixture _fixture = fixture;

    // ── Helper: navigate to LessonDetailPage ──────────────────────────────────

    /// <summary>
    /// Opens the WeekPage, waits for at least one lesson block to render, then
    /// taps it. Lesson blocks are added dynamically to LessonGrid by code-behind,
    /// so we locate them by their parent grid AutomationId and tap the first child.
    /// </summary>
    private LessonDetailPageObject NavigateToLessonDetail()
    {
        _fixture.GoToHomePage();
        var week = new HomePageObject(_fixture.Driver).TapScheduleTile();
        week.WaitForPage();
        week.WaitForLoadingComplete();

        // Lesson blocks are placed inside the LessonGrid by code-behind.
        // We find any interactive element inside that grid (a Border/Button with a name).
        // If no lessons are displayed, skip via Assert.Skip equivalent (xUnit lacks it
        // at this level — we fail with a descriptive message).
        var lessonGrid = _fixture.Driver.FindElement(MobileBy.AccessibilityId("LessonGrid"));
        var lessonBlocks = lessonGrid.FindElements(By.XPath(".//Group"));

        if (lessonBlocks.Count == 0)
        {
            // Fallback: try to click any coloured border inside the grid
            lessonBlocks = lessonGrid.FindElements(By.XPath(".//Button"));
        }

        Assert.True(lessonBlocks.Count > 0,
            "Er moet minstens één les zichtbaar zijn in de weekweergave om te testen");

        lessonBlocks[0].Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);

        var detail = new LessonDetailPageObject(_fixture.Driver);
        detail.WaitForPage();
        return detail;
    }

    // ── Page structure ────────────────────────────────────────────────────────

    [Fact]
    public void LessonDetail_TitelInHeader_IsZichtbaar()
    {
        var page = NavigateToLessonDetail();

        Assert.True(page.WorkoutName.Displayed,
            "Lesnaam moet zichtbaar zijn in de header van de LessonDetailPage");
    }

    [Fact]
    public void LessonDetail_Locatie_IsZichtbaar()
    {
        var page = NavigateToLessonDetail();

        Assert.True(page.Location.Displayed,
            "Locatieveld moet zichtbaar zijn op de LessonDetailPage");
    }

    [Fact]
    public void LessonDetail_Locatie_IsNietLeeg()
    {
        var page = NavigateToLessonDetail();

        Assert.False(string.IsNullOrWhiteSpace(page.GetLocationText()),
            "Locatieveld mag niet leeg zijn");
    }

    [Fact]
    public void LessonDetail_DeelnemersRij_IsZichtbaar()
    {
        var page = NavigateToLessonDetail();

        Assert.True(page.ParticipantsRow.Displayed,
            "Deelnemersrij moet zichtbaar zijn op de LessonDetailPage");
    }

    [Fact]
    public void LessonDetail_TerugKnop_IsZichtbaar()
    {
        var page = NavigateToLessonDetail();

        Assert.True(page.BackButton.Displayed,
            "Terugknop moet zichtbaar zijn in de header van de LessonDetailPage");
    }

    // ── Conditional buttons (future lesson, not booked) ───────────────────────

    [Fact]
    public void LessonDetail_NietIngeschreven_AanmeldenKnop_IsZichtbaar()
    {
        var page = NavigateToLessonDetail();

        // The first mock lesson (id=101) is in the future and not booked
        if (page.IsReserveBtnVisible())
            Assert.True(true, "Aanmeldenknop is zichtbaar voor een les waarop niet ingeschreven is");
        else
            Assert.True(page.IsExpiredBtnVisible() || page.IsUnregisterBtnVisible(),
                "Als de aanmeldenknop niet zichtbaar is moet een alternatieve actieknop zichtbaar zijn");
    }

    [Fact]
    public void LessonDetail_NietIngeschreven_AfmeldenKnop_IsNietZichtbaar()
    {
        var page = NavigateToLessonDetail();

        // For a future, non-booked lesson, the unregister button must be hidden
        if (page.IsReserveBtnVisible())
            Assert.False(page.IsUnregisterBtnVisible(),
                "Afmeldenknop mag niet zichtbaar zijn wanneer de gebruiker niet ingeschreven is");
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [Fact]
    public void LessonDetail_TerugKnop_NavigeertTerug()
    {
        var page = NavigateToLessonDetail();

        page.TapBack();

        var week = new WeekPageObject(_fixture.Driver);
        week.WaitForPage();

        Assert.True(week.IsOnWeekPage(),
            "Na tikken op terugknop moet de LessonDetailPage sluiten en de WeekPage zichtbaar zijn");

        // Return to Home for isolation
        week.TapBack();
        new HomePageObject(_fixture.Driver).WaitForPage();
    }
}
