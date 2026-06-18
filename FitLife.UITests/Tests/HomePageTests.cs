namespace FitLife.UITests.Tests;

/// <summary>
/// Cross-cutting — Home/dashboard navigatie.
/// De homepage is de toegangspoort tot de stories: de ledentegels leiden naar het
/// rooster (US-L01), reserveringen (US-L05), profiel (US-L06) en abonnement (US-L06).
/// Each test navigates back to the Home page before running (via GoToHomePage)
/// so tests are independent even though they share the same app session.
/// </summary>
[Trait("UserStory", "Cross-cutting")]
[Collection(UITestCollection.Name)]
public sealed class HomePageTests(AppiumFixture fixture)
{
    private readonly AppiumFixture _fixture = fixture;

    // ── Page structure ────────────────────────────────────────────────────────

    [Fact]
    public void HomePage_LogoZichtbaar_NaLogin()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.Logo.Displayed,
            "FitLife-logo moet zichtbaar zijn op de homepage na inloggen");
    }

    [Fact]
    public void HomePage_WelkomsTekst_IsZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.WelcomeLabel.Displayed,
            "Welkomstbericht moet zichtbaar zijn op de homepage");
    }

    [Fact]
    public void HomePage_HamburgerKnop_IsZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.HamburgerButton.Displayed,
            "Hamburger-menuknop moet zichtbaar zijn in de header");
    }

    [Fact]
    public void HomePage_NotificatieBel_IsZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.NotificationBell.Displayed,
            "Notificatiebel moet zichtbaar zijn in de header");
    }

    // ── Member tiles ──────────────────────────────────────────────────────────

    [Fact]
    public void HomePage_LidTegels_ZijnZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.MemberTilesGrid.Displayed,
            "Ledentegels moeten zichtbaar zijn voor een lid-account");
    }

    [Fact]
    public void HomePage_RoosterTegel_IsZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.TileSchedule.Displayed,
            "Rooster-tegel moet zichtbaar zijn");
    }

    [Fact]
    public void HomePage_ReservationsTegel_IsZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.TileReservations.Displayed,
            "Reserveringen-tegel moet zichtbaar zijn");
    }

    [Fact]
    public void HomePage_ProfielTegel_IsZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.TileProfile.Displayed,
            "Profiel-tegel moet zichtbaar zijn");
    }

    [Fact]
    public void HomePage_AbonnementTegel_IsZichtbaar()
    {
        _fixture.GoToHomePage();

        var page = new HomePageObject(_fixture.Driver);

        Assert.True(page.TileSubscription.Displayed,
            "Abonnement-tegel moet zichtbaar zijn");
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("UserStory", "US-L01")]
    public void HomePage_TikOpRoosterTegel_NavigeertNaarWeekPagina()
    {
        _fixture.GoToHomePage();

        var home = new HomePageObject(_fixture.Driver);
        var week = home.TapScheduleTile();

        week.WaitForPage();

        Assert.True(week.IsOnWeekPage(),
            "Na tikken op de Rooster-tegel moet de WeekPage geopend worden");

        // Navigate back for isolation
        week.TapBack();
        new HomePageObject(_fixture.Driver).WaitForPage();
    }

    [Fact]
    [Trait("UserStory", "US-L06")]
    public void HomePage_TikOpProfielTegel_NavigeertNaarProfielPagina()
    {
        _fixture.GoToHomePage();

        var home    = new HomePageObject(_fixture.Driver);
        var profile = home.TapProfileTile();

        profile.WaitForPage();

        Assert.True(profile.IsOnProfilePage(),
            "Na tikken op de Profiel-tegel moet de ProfielPagina geopend worden");

        // Navigate back for isolation
        profile.TapBack();
        new HomePageObject(_fixture.Driver).WaitForPage();
    }
}
