using FitLife.Maui.Services;
using FitLife.Tests.Fakes;

namespace FitLife.Tests.Authentication;

/// <summary>
/// Cross-cutting — Authenticatie & sessie (inloggen, uitloggen, rollen).
/// Login onderbouwt alle user stories: het levert userId, rol, credits en
/// abonnement waarop de stories verder bouwen. Specifieke acceptatiecriteria
/// zijn extra getagd:
///   • US-L06 — credits/abonnement zichtbaar na inloggen.
///   • US-L07 — profielfoto gekoppeld aan het account.
/// Rollen: voor nu telt een Beheerder (admin) als Instructeur (zelfde functie).
/// </summary>
[Trait("UserStory", "Cross-cutting")]
public class AuthenticationServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static HttpClient JsonClient(bool success, string role = "member", int userId = 1,
        string displayName = "Test User", int credits = 13, string subscriptionType = "Intermediate")
    {
        var body = new LoginResponse
        {
            Success             = success,
            UserId              = success ? userId         : null,
            DisplayName         = success ? displayName    : null,
            Role                = success ? role           : null,
            Credits             = success ? credits        : null,
            SubscriptionType    = success ? subscriptionType : null,
            Message             = success ? "Login geslaagd" : "Ongeldige inloggegevens"
        };
        return MockHttpMessageHandler.CreateJsonClient(body);
    }

    // ── Inloggen: happy flow ──────────────────────────────────────────────────

    [Fact]
    public async Task Login_MetGeldigeGegevens_GeeftSuccesTerug()
    {
        var service = new AuthenticationService(JsonClient(true));
        var result = await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Login_MetGeldigeGegevens_StelIsAuthenticatedIn()
    {
        var service = new AuthenticationService(JsonClient(true));
        await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public async Task Login_MetGeldigeGegevens_VultCurrentUserIdIn()
    {
        var service = new AuthenticationService(JsonClient(true, userId: 42));
        await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.Equal(42, service.CurrentUserId);
    }

    [Fact]
    public async Task Login_MetGeldigeGegevens_VultDisplayNameIn()
    {
        var service = new AuthenticationService(JsonClient(true, displayName: "Yoran"));
        await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.Equal("Yoran", service.CurrentUserName);
    }

    [Fact]
    [Trait("UserStory", "US-L06")]
    public async Task Login_MetGeldigeGegevens_VultCreditsIn()
    {
        var service = new AuthenticationService(JsonClient(true, credits: 9));
        await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.Equal(9, service.CurrentUserCredits);
    }

    [Fact]
    [Trait("UserStory", "US-L06")]
    public async Task Login_MetGeldigeGegevens_VultAbonnementTypeIn()
    {
        var service = new AuthenticationService(JsonClient(true, subscriptionType: "Rookie"));
        await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.Equal("Rookie", service.CurrentUserSubscriptionType);
    }

    // ── Inloggen: foutgevallen ────────────────────────────────────────────────

    [Fact]
    public async Task Login_MetVerkeerdeGegevens_GeeftMislukteResultaat()
    {
        var service = new AuthenticationService(JsonClient(false));
        var result = await service.LoginAsync("fout@example.com", "verkeerd");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Login_MetVerkeerdeGegevens_StelIsAuthenticatedNietIn()
    {
        var service = new AuthenticationService(JsonClient(false));
        await service.LoginAsync("fout@example.com", "verkeerd");
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task Login_MetVerkeerdeGegevens_LaatFoutberichtZien()
    {
        var service = new AuthenticationService(JsonClient(false));
        var result = await service.LoginAsync("fout@example.com", "verkeerd");
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task Login_BijNetwerkfout_GeeftMislukteResultaatZonderException()
    {
        var service = new AuthenticationService(MockHttpMessageHandler.CreateNetworkErrorClient());
        var result = await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task Login_BijServerFout500_GeeftMislukteResultaat()
    {
        var service = new AuthenticationService(
            MockHttpMessageHandler.CreateErrorClient(System.Net.HttpStatusCode.InternalServerError));
        var result = await service.LoginAsync("user@example.com", "wachtwoord");
        Assert.False(result.Success);
    }

    // ── Toegangsrechten en rollen ─────────────────────────────────────────────
    // Rollen uit de user stories: Lid, Instructeur en Beheerder.
    // Voor nu heeft een Beheerder (admin) EXACT dezelfde functie als een trainer,
    // dus IsInstructor is true voor zowel "instructor" als "admin" en zij zien
    // dezelfde staf-weergave. Een admin is daardoor nooit een "member".

    [Fact]
    public async Task Login_AlsAdmin_TeltAlsInstructeur()
    {
        var service = new AuthenticationService(JsonClient(true, role: "admin"));
        await service.LoginAsync("admin@example.com", "wachtwoord");
        Assert.True(service.IsAdmin);
        Assert.True(service.IsInstructor);   // admin == trainer: zelfde functie
        Assert.False(service.IsMember);
    }

    [Fact]
    public async Task Login_AlsInstructor_StelIsInstructorIn()
    {
        var service = new AuthenticationService(JsonClient(true, role: "instructor"));
        await service.LoginAsync("trainer@example.com", "wachtwoord");
        Assert.False(service.IsAdmin);
        Assert.True(service.IsInstructor);
        Assert.False(service.IsMember);
    }

    [Fact]
    public async Task Login_AlsLid_StelIsMemberIn()
    {
        var service = new AuthenticationService(JsonClient(true, role: "member"));
        await service.LoginAsync("lid@example.com", "wachtwoord");
        Assert.False(service.IsAdmin);
        Assert.False(service.IsInstructor);
        Assert.True(service.IsMember);
    }

    [Fact]
    public async Task RolVergelijking_IsCaseInsensitive()
    {
        var service = new AuthenticationService(JsonClient(true, role: "ADMIN"));
        await service.LoginAsync("admin@example.com", "wachtwoord");
        Assert.True(service.IsAdmin);
        Assert.True(service.IsInstructor);   // hoofdletterongevoelig, en admin == trainer
    }

    // ── Uitloggen ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Uitloggen_NaInloggen_WischtIsAuthenticated()
    {
        var service = new AuthenticationService(JsonClient(true));
        await service.LoginAsync("user@example.com", "wachtwoord");
        await service.LogoutAsync();
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task Uitloggen_NaInloggen_WischtCurrentUserId()
    {
        var service = new AuthenticationService(JsonClient(true, userId: 5));
        await service.LoginAsync("user@example.com", "wachtwoord");
        await service.LogoutAsync();
        Assert.Null(service.CurrentUserId);
    }

    [Fact]
    public async Task Uitloggen_NaInloggen_WischtCurrentUserName()
    {
        var service = new AuthenticationService(JsonClient(true, displayName: "Yoran"));
        await service.LoginAsync("user@example.com", "wachtwoord");
        await service.LogoutAsync();
        Assert.Null(service.CurrentUserName);
    }

    [Fact]
    public async Task Uitloggen_NaInloggen_WischtCredits()
    {
        var service = new AuthenticationService(JsonClient(true, credits: 13));
        await service.LoginAsync("user@example.com", "wachtwoord");
        await service.LogoutAsync();
        Assert.Null(service.CurrentUserCredits);
    }

    // ── Beginstatus (voor inloggen) ───────────────────────────────────────────

    [Fact]
    public void VoorInloggen_IsAuthenticated_IsFalse()
    {
        var service = new AuthenticationService(JsonClient(true));
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void VoorInloggen_CurrentUserId_IsNull()
    {
        var service = new AuthenticationService(JsonClient(true));
        Assert.Null(service.CurrentUserId);
    }

    [Fact]
    public void VoorInloggen_CurrentUserName_IsNull()
    {
        var service = new AuthenticationService(JsonClient(true));
        Assert.Null(service.CurrentUserName);
    }

    [Fact]
    public void VoorInloggen_IsMember_IsTrue_StandaardRol()
    {
        // Zonder ingelogde gebruiker: niet admin en niet instructor → IsMember is true
        var service = new AuthenticationService(JsonClient(true));
        Assert.True(service.IsMember);
    }

    // ── Sessieherstel (opnieuw inloggen na uitloggen) ─────────────────────────

    [Fact]
    public async Task NaUitloggenOpnieuwInloggen_HersteltSessie()
    {
        var service = new AuthenticationService(JsonClient(true, userId: 7, displayName: "Heraangemeld"));
        await service.LoginAsync("user@example.com", "wachtwoord");
        await service.LogoutAsync();

        await service.LoginAsync("user@example.com", "wachtwoord");

        Assert.True(service.IsAuthenticated);
        Assert.Equal(7, service.CurrentUserId);
        Assert.Equal("Heraangemeld", service.CurrentUserName);
    }

    // ── Profielfoto (US-L07) ──────────────────────────────────────────────────

    [Fact]
    [Trait("UserStory", "US-L07")]
    public async Task UpdatePhotoUrl_NaInloggen_WijzigtGecachedeFotoUrl()
    {
        var service = new AuthenticationService(JsonClient(true));
        await service.LoginAsync("user@example.com", "wachtwoord");

        service.UpdatePhotoUrl("https://example.com/foto.jpg");

        Assert.Equal("https://example.com/foto.jpg", service.CurrentUserPhotoUrl);
    }

    [Fact]
    [Trait("UserStory", "US-L07")]
    public async Task UpdatePhotoUrl_MetNull_WischtFotoUrl()
    {
        var service = new AuthenticationService(JsonClient(true));
        await service.LoginAsync("user@example.com", "wachtwoord");
        service.UpdatePhotoUrl("https://example.com/foto.jpg");

        service.UpdatePhotoUrl(null);

        Assert.Null(service.CurrentUserPhotoUrl);
    }
}
