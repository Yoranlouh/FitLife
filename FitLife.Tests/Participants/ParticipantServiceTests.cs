using FitLife.Maui.Services;
using FitLife.Tests.Fakes;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Tests.Participants;

/// <summary>
/// US-I02 — Deelnemerslijst bekijken (Instructeur, Should have).
/// "Als instructeur wil ik de deelnemerslijst van een les kunnen bekijken, zodat
///  ik mij kan voorbereiden en weet wie er komt."
///
/// Acceptatiecriteria gedekt:
///   • De lijst toont de naam en eventueel de profielfoto van elke deelnemer
///     (Name + ImageUrl in ParticipantResponse).
///   • De wachtlijst is apart op te vragen (GetWaitlistAsync).
/// Bij een netwerkfout degradeert de UI netjes naar een lege lijst.
/// </summary>
[Trait("UserStory", "US-I02")]
[Trait("Rol", "Instructeur")]
public class ParticipantServiceTests
{
    // ── Deelnemers ophalen ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetParticipants_BijSucces_RetourneertDeelnemers()
    {
        var body = new[]
        {
            new ParticipantResponse { MemberId = 1, Name = "Jan Smit",     ImageUrl = "http://x/jan.jpg" },
            new ParticipantResponse { MemberId = 2, Name = "Marie de Vries", ImageUrl = null },
        };
        var service = new ParticipantService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = (await service.GetParticipantsAsync(lessonId: 101)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Jan Smit", result[0].Name);
    }

    [Fact]
    public async Task GetParticipants_ToontNaamEnProfielfoto()
    {
        var body = new[]
        {
            new ParticipantResponse { MemberId = 1, Name = "Jan Smit", ImageUrl = "http://x/jan.jpg" },
        };
        var service = new ParticipantService(MockHttpMessageHandler.CreateJsonClient(body));

        var deelnemer = (await service.GetParticipantsAsync(lessonId: 101)).Single();

        Assert.Equal("Jan Smit", deelnemer.Name);
        Assert.Equal("http://x/jan.jpg", deelnemer.ImageUrl);
    }

    [Fact]
    public async Task GetParticipants_BijNetwerkfout_RetourneertLegeLijst()
    {
        var service = new ParticipantService(MockHttpMessageHandler.CreateNetworkErrorClient());

        Assert.Empty(await service.GetParticipantsAsync(lessonId: 101));
    }

    [Fact]
    public async Task GetParticipants_BijHttpFout_RetourneertLegeLijst()
    {
        var service = new ParticipantService(
            MockHttpMessageHandler.CreateErrorClient(System.Net.HttpStatusCode.NotFound));

        Assert.Empty(await service.GetParticipantsAsync(lessonId: 101));
    }

    // ── Wachtlijst ophalen ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetWaitlist_BijSucces_RetourneertWachtlijst()
    {
        var body = new[]
        {
            new ParticipantResponse { MemberId = 9, Name = "Kees Bak" },
        };
        var service = new ParticipantService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = (await service.GetWaitlistAsync(lessonId: 101)).ToList();

        Assert.Single(result);
        Assert.Equal("Kees Bak", result[0].Name);
    }

    [Fact]
    public async Task GetWaitlist_BijNetwerkfout_RetourneertLegeLijst()
    {
        var service = new ParticipantService(MockHttpMessageHandler.CreateNetworkErrorClient());

        Assert.Empty(await service.GetWaitlistAsync(lessonId: 101));
    }
}
