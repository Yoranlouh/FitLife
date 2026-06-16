using FitLife.Maui.Services;

namespace FitLife.Tests.Attendance;

/// <summary>
/// Tests for MockAttendanceService — the in-memory check-in implementation
/// used as a stand-in for real RFID and GPS check-in during development.
/// </summary>
public class AttendanceServiceTests
{
    // ── IsCheckedIn: beginstatus ──────────────────────────────────────────────

    [Fact]
    public void IsCheckedIn_VoorCheckIn_IsAlwaysFalse()
    {
        IAttendanceService service = new MockAttendanceService();
        Assert.False(service.IsCheckedIn(lessonId: 1, userId: 10));
    }

    [Fact]
    public void IsCheckedIn_VoorCheckIn_VoorVerschillendeKombinaties_IsAlwaysFalse()
    {
        IAttendanceService service = new MockAttendanceService();
        Assert.False(service.IsCheckedIn(1, 1));
        Assert.False(service.IsCheckedIn(2, 1));
        Assert.False(service.IsCheckedIn(1, 2));
    }

    // ── Mock RFID check-in ────────────────────────────────────────────────────

    [Fact]
    public async Task CheckIn_ViaRfid_GeeftTrueTerug()
    {
        IAttendanceService service = new MockAttendanceService();
        var result = await service.CheckInAsync(lessonId: 1, userId: 10, CheckInMethod.Rfid);
        Assert.True(result);
    }

    [Fact]
    public async Task CheckIn_ViaRfid_StelIsCheckedInIn()
    {
        IAttendanceService service = new MockAttendanceService();
        await service.CheckInAsync(lessonId: 1, userId: 10, CheckInMethod.Rfid);
        Assert.True(service.IsCheckedIn(1, 10));
    }

    // ── Mock GPS check-in ─────────────────────────────────────────────────────

    [Fact]
    public async Task CheckIn_ViaGps_GeeftTrueTerug()
    {
        IAttendanceService service = new MockAttendanceService();
        var result = await service.CheckInAsync(lessonId: 2, userId: 20, CheckInMethod.Gps);
        Assert.True(result);
    }

    [Fact]
    public async Task CheckIn_ViaGps_StelIsCheckedInIn()
    {
        IAttendanceService service = new MockAttendanceService();
        await service.CheckInAsync(lessonId: 2, userId: 20, CheckInMethod.Gps);
        Assert.True(service.IsCheckedIn(2, 20));
    }

    // ── Isolatie: verschillende gebruikers en lessen ──────────────────────────

    [Fact]
    public async Task CheckIn_VanGebruiker1_PastvrijGebruiker2NietAan()
    {
        IAttendanceService service = new MockAttendanceService();
        await service.CheckInAsync(lessonId: 1, userId: 1, CheckInMethod.Rfid);

        Assert.False(service.IsCheckedIn(lessonId: 1, userId: 2));
    }

    [Fact]
    public async Task CheckIn_VoorLes1_PastvrijLes2NietAan()
    {
        IAttendanceService service = new MockAttendanceService();
        await service.CheckInAsync(lessonId: 1, userId: 1, CheckInMethod.Rfid);

        Assert.False(service.IsCheckedIn(lessonId: 2, userId: 1));
    }

    [Fact]
    public async Task MeerdereGebruikers_CheckInOnafhankelijkVanElkaar()
    {
        IAttendanceService service = new MockAttendanceService();
        await service.CheckInAsync(lessonId: 5, userId: 100, CheckInMethod.Rfid);
        await service.CheckInAsync(lessonId: 5, userId: 200, CheckInMethod.Gps);

        Assert.True(service.IsCheckedIn(5, 100));
        Assert.True(service.IsCheckedIn(5, 200));
        Assert.False(service.IsCheckedIn(5, 300));
    }

    [Fact]
    public async Task EenGebruiker_CanCheckInVoorMeerdereLessen()
    {
        IAttendanceService service = new MockAttendanceService();
        await service.CheckInAsync(lessonId: 10, userId: 1, CheckInMethod.Rfid);
        await service.CheckInAsync(lessonId: 11, userId: 1, CheckInMethod.Rfid);

        Assert.True(service.IsCheckedIn(10, 1));
        Assert.True(service.IsCheckedIn(11, 1));
    }

    // ── Idempotentie: twee keer inchecken ─────────────────────────────────────

    [Fact]
    public async Task CheckIn_TweeKeer_BlijftGeregistreerd_GeenFout()
    {
        IAttendanceService service = new MockAttendanceService();
        await service.CheckInAsync(lessonId: 1, userId: 1, CheckInMethod.Rfid);
        var second = await service.CheckInAsync(lessonId: 1, userId: 1, CheckInMethod.Rfid);

        Assert.True(second);
        Assert.True(service.IsCheckedIn(1, 1));
    }

    // ── Checkstatus na service-herstart (in-memory reset) ─────────────────────

    [Fact]
    public void NieuweServiceInstantie_HeeftGeenCheckIns()
    {
        // Elke keer een nieuwe MockAttendanceService → lege HashSet
        IAttendanceService service = new MockAttendanceService();
        Assert.False(service.IsCheckedIn(1, 1));
    }

    // ── Aanwezigheidsstatus correct registreren (gecombineerde flow) ──────────

    [Fact]
    public async Task VolledigeFlow_CheckIn_StatusKlopt()
    {
        IAttendanceService service = new MockAttendanceService();
        int lessonId = 42, userId = 7;

        Assert.False(service.IsCheckedIn(lessonId, userId), "Nog niet ingecheckt");

        var success = await service.CheckInAsync(lessonId, userId, CheckInMethod.Rfid);

        Assert.True(success, "CheckIn moet succes teruggeven");
        Assert.True(service.IsCheckedIn(lessonId, userId), "Na check-in moet IsCheckedIn true zijn");
    }
}
