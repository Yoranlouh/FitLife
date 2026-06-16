using FitLife.Maui.Services;
using FitLife.Tests.Fakes;

namespace FitLife.Tests.Reservations;

public class ReservationServiceTests
{
    // ── ReserveAsync ─────────────────────────────────────────────────────────

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

    // ── CancelReservationAsync ────────────────────────────────────────────────

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

    // ── JoinWaitlistAsync ─────────────────────────────────────────────────────

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

    // ── GetUserLessonsAsync ───────────────────────────────────────────────────

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
