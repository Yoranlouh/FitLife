namespace FitLife.UITests.Tests;

/// <summary>
/// US-L06 — Abonnement en credits inzien (Lid, Should have).
/// UI-test voor ProfilePage: abonnementsnaam en het actuele aantal credits zijn
/// zichtbaar. Bevat ook de knoppen voor profielfoto (US-L07) en Mijn lessen (US-L05).
/// Navigation path: Home → tap Profiel tile → ProfilePage.
/// The mock API returns a Rookie subscriber with 9 credits.
/// </summary>
[Trait("UserStory", "US-L06")]
[Trait("Rol", "Lid")]
[Collection(UITestCollection.Name)]
public sealed class ProfilePageTests(AppiumFixture fixture)
{
    private readonly AppiumFixture _fixture = fixture;

    // ── Helper: navigate to ProfilePage ───────────────────────────────────────

    private ProfilePageObject NavigateToProfilePage()
    {
        _fixture.GoToHomePage();
        var profile = new HomePageObject(_fixture.Driver).TapProfileTile();
        profile.WaitForPage();
        return profile;
    }

    // ── Page structure ────────────────────────────────────────────────────────

    [Fact]
    public void ProfielPagina_VoornaamLabel_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.FirstName.Displayed,
            "Voornaam-label moet zichtbaar zijn op de profielpagina");
    }

    [Fact]
    public void ProfielPagina_AchternaamLabel_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.LastName.Displayed,
            "Achternaam-label moet zichtbaar zijn op de profielpagina");
    }

    [Fact]
    public void ProfielPagina_AbonnementNaam_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.SubscriptionName.Displayed,
            "Abonnementsnaam moet zichtbaar zijn op de profielpagina");
    }

    [Fact]
    public void ProfielPagina_AbonnementNaam_IsNietLeeg()
    {
        var page = NavigateToProfilePage();

        Assert.False(string.IsNullOrWhiteSpace(page.GetSubscriptionName()),
            "Abonnementsnaam mag niet leeg zijn");
    }

    [Fact]
    public void ProfielPagina_CreditsWeergave_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.CreditsDisplay.Displayed,
            "Credits-weergave moet zichtbaar zijn op de profielpagina");
    }

    [Fact]
    public void ProfielPagina_CreditsWeergave_IsNietLeeg()
    {
        var page = NavigateToProfilePage();

        Assert.False(string.IsNullOrWhiteSpace(page.GetCreditsText()),
            "Credits-weergave mag niet leeg zijn");
    }

    // ── Action buttons ────────────────────────────────────────────────────────

    [Fact]
    [Trait("UserStory", "US-L07")]
    public void ProfielPagina_FotoWijzigenKnop_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.ChangePhotoButton.Displayed,
            "Foto-wijzigenknop moet zichtbaar zijn");
    }

    [Fact]
    public void ProfielPagina_AbonnementBeheerKnop_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.ManageSubscriptionBtn.Displayed,
            "Abonnement-beheerknop moet zichtbaar zijn");
    }

    [Fact]
    [Trait("UserStory", "US-L05")]
    public void ProfielPagina_MijnLessenKnop_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.ViewMyLessonsBtn.Displayed,
            "Mijn-lessenknop moet zichtbaar zijn");
    }

    [Fact]
    public void ProfielPagina_UitlogKnop_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.LogoutButton.Displayed,
            "Uitlogknop moet zichtbaar zijn op de profielpagina");
    }

    [Fact]
    public void ProfielPagina_TerugKnop_IsZichtbaar()
    {
        var page = NavigateToProfilePage();

        Assert.True(page.BackButton.Displayed,
            "Terugknop moet zichtbaar zijn in de header van de profielpagina");
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [Fact]
    public void ProfielPagina_TerugKnop_NavigeertTerug()
    {
        var page = NavigateToProfilePage();

        page.TapBack();

        var home = new HomePageObject(_fixture.Driver);
        home.WaitForPage();

        Assert.True(home.IsOnHomePage(),
            "Na tikken op terugknop moet de profielpagina sluiten en de HomePage zichtbaar zijn");
    }

    [Fact]
    public void ProfielPagina_VoornaamNietLeeg_TijdensHuidigeSessionb()
    {
        var page = NavigateToProfilePage();

        // The display name from the mock API is "Test Gebruiker"
        // so at least one of first/last name must be non-empty
        var first = page.GetFirstName();
        var last  = page.LastName.Text;

        Assert.True(
            !string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(last),
            "Ten minste voornaam of achternaam moet zichtbaar zijn");
    }
}
