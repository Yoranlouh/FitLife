namespace FitLife.UITests.Tests;

/// <summary>
/// US-L01 — Lesrooster bekijken (Lid, Must have).
/// UI-test voor de WeekPage (kalenderweergave): het dag-/weekoverzicht is zichtbaar,
/// laadt asynchroon (WaitForLoadingComplete) en is per week te navigeren.
/// Navigates from the Home page via the Rooster tile before each test.
/// </summary>
[Trait("UserStory", "US-L01")]
[Trait("Rol", "Lid")]
[Collection(UITestCollection.Name)]
public sealed class WeekPageTests(AppiumFixture fixture)
{
    private readonly AppiumFixture _fixture = fixture;

    // ── Helper: navigate to WeekPage from a clean state ───────────────────────

    private WeekPageObject NavigateToWeekPage()
    {
        _fixture.GoToHomePage();
        var week = new HomePageObject(_fixture.Driver).TapScheduleTile();
        week.WaitForPage();
        return week;
    }

    // ── Page structure ────────────────────────────────────────────────────────

    [Fact]
    public void WeekPage_WeekRangeTekst_IsZichtbaar()
    {
        var page = NavigateToWeekPage();

        Assert.True(page.WeekRangeLabel.Displayed,
            "Weekbereiktekst moet zichtbaar zijn op de WeekPage");
    }

    [Fact]
    public void WeekPage_WeekRangeTekst_IsNietLeeg()
    {
        var page = NavigateToWeekPage();

        var text = page.GetWeekRangeText();

        Assert.False(string.IsNullOrWhiteSpace(text),
            "Weekbereiktekst mag niet leeg zijn");
    }

    [Fact]
    public void WeekPage_VoigeWeekKnop_IsZichtbaar()
    {
        var page = NavigateToWeekPage();

        Assert.True(page.PrevButton.Displayed,
            "Vorige-weekknop moet zichtbaar zijn");
    }

    [Fact]
    public void WeekPage_VolgendeWeekKnop_IsZichtbaar()
    {
        var page = NavigateToWeekPage();

        Assert.True(page.NextButton.Displayed,
            "Volgende-weekknop moet zichtbaar zijn");
    }

    [Fact]
    public void WeekPage_LijstweergaveKnop_IsZichtbaar()
    {
        var page = NavigateToWeekPage();

        Assert.True(page.ListViewButton.Displayed,
            "Lijstweergave-knop moet zichtbaar zijn");
    }

    [Fact]
    public void WeekPage_TerugKnop_IsZichtbaar()
    {
        var page = NavigateToWeekPage();

        Assert.True(page.BackButton.Displayed,
            "Terugknop moet zichtbaar zijn in de header van de WeekPage");
    }

    // ── Week navigation ───────────────────────────────────────────────────────

    [Fact]
    public void WeekPage_VorigeWeek_WijzigtWeekRangeTekst()
    {
        var page = NavigateToWeekPage();

        var originalText = page.GetWeekRangeText();
        page.TapPreviousWeek();
        page.WaitForLoadingComplete();
        var newText = page.GetWeekRangeText();

        Assert.NotEqual(originalText, newText);
    }

    [Fact]
    public void WeekPage_VolgendeWeek_WijzigtWeekRangeTekst()
    {
        var page = NavigateToWeekPage();

        var originalText = page.GetWeekRangeText();
        page.TapNextWeek();
        page.WaitForLoadingComplete();
        var newText = page.GetWeekRangeText();

        Assert.NotEqual(originalText, newText);
    }

    [Fact]
    public void WeekPage_VorigeEnVolgendeWeek_GeeftOrigineleTekstTerug()
    {
        var page = NavigateToWeekPage();

        var originalText = page.GetWeekRangeText();
        page.TapPreviousWeek();
        page.WaitForLoadingComplete();
        page.TapNextWeek();
        page.WaitForLoadingComplete();

        Assert.Equal(originalText, page.GetWeekRangeText());
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [Fact]
    public void WeekPage_TerugKnop_NavigeertTerug()
    {
        var page = NavigateToWeekPage();

        page.TapBack();

        var home = new HomePageObject(_fixture.Driver);
        home.WaitForPage();

        Assert.True(home.IsOnHomePage(),
            "Na tikken op de terugknop moet de WeekPage sluiten en de HomePage zichtbaar zijn");
    }
}