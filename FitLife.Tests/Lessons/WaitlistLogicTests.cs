using FitLife.Maui.Services;
using FitLife.Tests.Fakes;

namespace FitLife.Tests.Lessons;

/// <summary>
/// US-L04 — Op de wachtlijst plaatsen (Lid, Should have).
/// "Als lid wil ik mij op de wachtlijst van een volle les kunnen plaatsen, zodat
///  ik automatisch een plek krijg als er iemand annuleert."
///
/// Acceptatiecriteria gedekt:
///   • Op de wachtlijst plaatsen kan alleen wanneer de les vol is (vol → JoinWaitlist-beslissing).
///   • Het lid ziet zijn positie op de wachtlijst                 (position = 1, 2, …).
/// De automatische doorschuif- en notificatie-criteria zitten in de API/notificatielaag
/// (zie NotificationIconTests, US-B05/US-L04 melding).
/// </summary>
[Trait("UserStory", "US-L04")]
[Trait("Rol", "Lid")]
public class WaitlistLogicTests
{
    // ── Les vol → LessonFull vlag ─────────────────────────────────────────────

    [Fact]
    public async Task Reserve_BijVolLes_StelLessonFullIn()
    {
        var body = new { success = false, lessonFull = true, message = "Les is vol." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.True(result.LessonFull);
    }

    [Fact]
    public async Task Reserve_BijNormaaleMislukking_StelLessonFullNietIn()
    {
        var body = new { success = false, lessonFull = false, message = "Onvoldoende credits." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.ReserveAsync(lessonId: 1, userId: 10);

        Assert.False(result.LessonFull);
    }

    // ── Wachtlijst toevoegen: succesflow ──────────────────────────────────────

    [Fact]
    public async Task WachtlijstToevoegen_GeeftSuccesEnPositie()
    {
        var body = new { success = true, position = 1, message = "Toegevoegd op positie 1." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.True(result.Success);
        Assert.Equal(1, result.Position);
    }

    [Fact]
    public async Task WachtlijstToevoegen_TweedeGebruiker_KrijgtPositie2()
    {
        var body = new { success = true, position = 2, message = "Toegevoegd op positie 2." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 20);

        Assert.True(result.Success);
        Assert.Equal(2, result.Position);
    }

    // ── Wachtlijst: al op wachtlijst ──────────────────────────────────────────

    [Fact]
    public async Task WachtlijstToevoegen_AlOpWachtlijst_StelAlreadyOnWaitlistIn()
    {
        var body = new { success = false, alreadyOnWaitlist = true, message = "Al op wachtlijst." };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.True(result.AlreadyOnWaitlist);
    }

    [Fact]
    public async Task WachtlijstToevoegen_AlOpWachtlijst_LessonFullIsNietGezet()
    {
        var body = new { success = false, alreadyOnWaitlist = true };
        var service = new ReservationService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.False(result.LessonFull);
    }

    // ── Wachtlijst: foutgevallen ──────────────────────────────────────────────

    [Fact]
    public async Task WachtlijstToevoegen_BijNetwerkfout_GeeftMislukteResultaat()
    {
        var service = new ReservationService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
        Assert.False(result.AlreadyOnWaitlist);
    }

    [Fact]
    public async Task WachtlijstToevoegen_BijServerFout_GeeftMislukteResultaat()
    {
        var service = new ReservationService(
            MockHttpMessageHandler.CreateErrorClient(System.Net.HttpStatusCode.InternalServerError));

        var result = await service.JoinWaitlistAsync(lessonId: 1, userId: 10);

        Assert.False(result.Success);
    }

    // ── Beslissingslogica: vol → wachtlijst (pure logica) ────────────────────

    [Theory]
    [InlineData(true, false, WaitlistDecision.JoinWaitlist)]
    [InlineData(false, true, WaitlistDecision.Success)]
    [InlineData(false, false, WaitlistDecision.ShowError)]
    public void WachtlijstBeslissing_BasedOpReserveResultaat_CorrectBeslissing(
        bool lessonFull, bool reserveSuccess, WaitlistDecision expected)
    {
        var decision = WaitlistLogic.DecideAfterReserve(success: reserveSuccess, lessonFull: lessonFull);
        Assert.Equal(expected, decision);
    }
}

public enum WaitlistDecision { Success, JoinWaitlist, ShowError }

internal static class WaitlistLogic
{
    public static WaitlistDecision DecideAfterReserve(bool success, bool lessonFull)
    {
        if (success) return WaitlistDecision.Success;
        if (lessonFull) return WaitlistDecision.JoinWaitlist;
        return WaitlistDecision.ShowError;
    }
}
