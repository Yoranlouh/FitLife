using FitLife.Maui.Services;
using FitLife.Tests.Fakes;

namespace FitLife.Tests.Reservations;

// Tests for ReservationService. The service backs four Lid-stories, so the tests
// are split into one [Trait]-tagged class per story:
//   • US-L02 — Les reserveren
//   • US-L03 — Reservering annuleren
//   • US-L04 — Op de wachtlijst plaatsen
//   • US-L05 — Mijn reserveringen bekijken

/// <summary>
/// US-L02 — Les reserveren (Lid, Must have).
/// "Als lid wil ik een les kunnen reserveren, zodat ik verzekerd ben van een plek
///  en één credit van mijn abonnement wordt afgeschreven."
///
/// Acceptatiecriteria gedekt:
///   • Bij succes ziet het lid een bevestiging en de bijgewerkte creditbalans (RemainingCredits).
///   • Reserveren is niet mogelijk wanneer de les vol is (LessonFull-vlag).
///   • Een lid kan zich niet twee keer voor dezelfde les inschrijven (dubbele inschrijving).
/// </summary>
[Trait("UserStory", "US-L02")]
[Trait("Rol", "Lid")]
public class ReserveLessonTests
{
    [Fact]
    public async Task Reserve_BijSucces_GeeftSuccesResultaatTerug()
    {
        var body = new { success = true, message = "Reservering geslaagd!", remainingCredits = 12 };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Reserve_BijSucces_VultRemainingCreditsIn()
    {
        var body = new { success = true, message = "OK", remainingCredits = 8 };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.Equal(8, result.RemainingCredits);
    }

    [Fact]
    public async Task Reserve_BijLesVolGeldt_StelLessonFullIn()
    {
        var body = new { success = false, lessonFull = true, message = "Les is vol." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.True(result.LessonFull);
    }

    [Fact]
    public async Task Reserve_BijOnvoldoendeCredits_GeeftMislukteResultaat()
    {
        var body = new { success = false, message = "Onvoldoende credits." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.False(result.LessonFull);
    }

    [Fact]
    public async Task Reserve_BijHttpFout_GeeftMislukteResultaatZonderException()
    {
        var service = new ReservationService(
            MockHttpMessageHandler.CreateErrorClient(System.Net.HttpStatusCode.InternalServerError));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Reserve_BijNetwerkfout_GeeftMislukteResultaatZonderException()
    {
        var service = new ReservationService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.Contains("Netwerkfout", result.Message);
    }

    // ── Dubbele inschrijving: ReservationActionResult vlag ────────────────────

    [Fact]
    public async Task DubbeleInschrijving_GeeftMislukteResultaat()
    {
        var body = new { success = false, message = "Je bent al aangemeld voor deze les." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.False(result.LessonFull);
        Assert.False(result.AlreadyOnWaitlist);
    }

    // ── Reserveringstatus: resultaatvelden correct ingesteld ──────────────────

    [Fact]
    public void ReservationActionResult_StandaardWaarden_ZijnFalse()
    {
        var result = new ReservationActionResult();
        Assert.False(result.Success);
        Assert.False(result.LessonFull);
        Assert.False(result.AlreadyOnWaitlist);
        Assert.Null(result.RemainingCredits);
        Assert.Null(result.Position);
    }
}

/// <summary>
/// US-L03 — Reservering annuleren (Lid, Must have).
/// "Als lid wil ik een reservering kunnen annuleren, zodat mijn credit wordt
///  teruggestort en mijn plek vrijkomt voor anderen."
///
/// Acceptatiecriteria gedekt:
///   • Bij annulering wordt de credit teruggestort (RemainingCredits stijgt).
///   • Het lid ontvangt een bevestiging van de annulering (Success/Message).
/// </summary>
[Trait("UserStory", "US-L03")]
[Trait("Rol", "Lid")]
public class CancelReservationTests
{
    [Fact]
    public async Task Annuleren_BijSucces_GeeftSuccesResultaatTerug()
    {
        var body = new { success = true, message = "Reservering geannuleerd!", remainingCredits = 10 };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.CancelReservationAsync(lessonId: 1, userId: 10);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Annuleren_BijSucces_VultTeruggegevenCreditsIn()
    {
        var body = new { success = true, message = "OK", remainingCredits = 13 };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.CancelReservationAsync(lessonId: 1, userId: 10);

        Assert.Equal(13, result.RemainingCredits);
    }

    [Fact]
    public async Task Annuleren_ZonderActieveReservering_GeeftMislukteResultaat()
    {
        var body = new { success = false, message = "Geen actieve reservering gevonden." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.CancelReservationAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Annuleren_BijNetwerkfout_GeeftMislukteResultaatZonderException()
    {
        var service = new ReservationService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var result = await service.CancelReservationAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.Contains("Netwerkfout", result.Message);
    }
}

/// <summary>
/// US-L04 — Op de wachtlijst plaatsen (Lid, Should have).
/// Service-zijde van het wachtlijstproces: toevoegen geeft de positie terug, en
/// een tweede poging meldt dat het lid al op de wachtlijst staat.
/// </summary>
[Trait("UserStory", "US-L04")]
[Trait("Rol", "Lid")]
public class JoinWaitlistTests
{
    [Fact]
    public async Task WachtlijstToevoegen_BijSucces_GeeftSuccesTerug()
    {
        var body = new { success = true, position = 2, message = "Toegevoegd aan wachtlijst." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task WachtlijstToevoegen_BijSucces_VultPositieIn()
    {
        var body = new { success = true, position = 3, message = "OK" };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.Equal(3, result.Position);
    }

    [Fact]
    public async Task WachtlijstToevoegen_AlsAlOpWachtlijst_StelAlreadyOnWaitlistIn()
    {
        var body = new { success = false, alreadyOnWaitlist = true, message = "Al op wachtlijst." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.True(result.AlreadyOnWaitlist);
    }

    [Fact]
    public async Task WachtlijstToevoegen_BijNetwerkfout_GeeftMislukteResultaatZonderException()
    {
        var service = new ReservationService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
    }
}

/// <summary>
/// US-L05 — Mijn reserveringen bekijken (Lid, Should have).
/// "Als lid wil ik een overzicht van mijn komende en afgelopen reserveringen zien."
/// GetUserLessonsAsync levert de lessen waarvoor het lid is ingeschreven.
/// </summary>
[Trait("UserStory", "US-L05")]
[Trait("Rol", "Lid")]
public class MyReservationsTests
{
    [Fact]
    public async Task Gebruikerslessen_BijSucces_RetourneertLessens()
    {
        var body = new[]
        {
            new { id = 1, workoutName = "Yoga", startTime = DateTime.Today.AddHours(9),
                  instructorName = "Lisa", locationName = "Zaal A" },
            new { id = 2, workoutName = "Spinning", startTime = DateTime.Today.AddHours(11),
                  instructorName = "Mark", locationName = "Zaal B" }
        };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var lessons = await service.GetUserLessonsAsync(userId: 1);

        Assert.Equal(2, lessons.Count());
    }

    [Fact]
    public async Task Gebruikerslessen_BijNetwerkfout_RetourneertLegeReeks()
    {
        var service = new ReservationService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var lessons = await service.GetUserLessonsAsync(userId: 1);

        Assert.Empty(lessons);
    }

    [Fact]
    public async Task Gebruikerslessen_BijHttpFout_RetourneertLegeReeks()
    {
        var service = new ReservationService(
            MockHttpMessageHandler.CreateErrorClient(System.Net.HttpStatusCode.NotFound));

        var lessons = await service.GetUserLessonsAsync(userId: 1);

        Assert.Empty(lessons);
    }
}
