using FitLife.Maui.Services;
using FitLife.Tests.Fakes;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Tests.Lessons;

public class LessonManagementServiceTests
{
    // ── Lessen ophalen (instructeur) ─────────────────────────────────────────

    [Fact]
    public async Task GetInstructeurLessen_BijSucces_RetourneertLessen()
    {
        var body = new[]
        {
            new LessonResponse { Id = 1, WorkoutName = "Yoga",     StartTime = DateTime.Today.AddHours(9) },
            new LessonResponse { Id = 2, WorkoutName = "Spinning", StartTime = DateTime.Today.AddHours(11) },
        };
        var service = new LessonManagementService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = (await service.GetInstructorLessonsAsync(instructorId: 5)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Yoga", result[0].WorkoutName);
    }

    [Fact]
    public async Task GetInstructeurLessen_BijNetwerkfout_RetourneertLegeReeks()
    {
        var service = new LessonManagementService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var result = await service.GetInstructorLessonsAsync(instructorId: 5);

        Assert.Empty(result);
    }

    // ── Les aanmaken ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LesAanmaken_BijSucces_GeeftSuccesTerug()
    {
        var body = new ApiResult { Success = true, Message = "Les succesvol aangemaakt." };
        var service = new LessonManagementService(MockHttpMessageHandler.CreateJsonClient(body));
        var request = new LessonSaveRequest
        {
            WorkoutId    = 1,
            InstructorId = 2,
            LocationId   = 3,
            StartTime    = DateTime.Today.AddHours(9),
            EndTime      = DateTime.Today.AddHours(10),
            MaxParticipants = 15
        };

        var (success, _) = await service.CreateLessonAsync(request);

        Assert.True(success);
    }

    [Fact]
    public async Task LesAanmaken_BijNetwerkfout_GeeftMislukteResultaat()
    {
        var service = new LessonManagementService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var (success, message) = await service.CreateLessonAsync(new LessonSaveRequest());

        Assert.False(success);
        Assert.Contains("Netwerkfout", message);
    }

    // ── Les wijzigen ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LesWijzigen_BijSucces_GeeftSuccesTerug()
    {
        var body = new ApiResult { Success = true, Message = "Les bijgewerkt." };
        var service = new LessonManagementService(MockHttpMessageHandler.CreateJsonClient(body));

        var (success, _) = await service.UpdateLessonAsync(lessonId: 1, new LessonSaveRequest());

        Assert.True(success);
    }

    [Fact]
    public async Task LesWijzigen_BijNetwerkfout_GeeftMislukteResultaat()
    {
        var service = new LessonManagementService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var (success, message) = await service.UpdateLessonAsync(1, new LessonSaveRequest());

        Assert.False(success);
        Assert.Contains("Netwerkfout", message);
    }

    // ── Les verwijderen (annuleren) ───────────────────────────────────────────

    [Fact]
    public async Task LesVerwijderen_BijSucces_GeeftSuccesTerug()
    {
        var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        var handler = new MockHttpMessageHandlerFactory(_ => response);
        var service = new LessonManagementService(handler.CreateClient());

        var (success, _) = await service.DeleteLessonAsync(lessonId: 1);

        Assert.True(success);
    }

    [Fact]
    public async Task LesVerwijderen_MetActieveReserveringen_GeeftMislukteResultaat()
    {
        var service = new LessonManagementService(
            MockHttpMessageHandler.CreateErrorClient(System.Net.HttpStatusCode.Conflict));

        var (success, _) = await service.DeleteLessonAsync(lessonId: 1);

        Assert.False(success);
    }

    [Fact]
    public async Task LesVerwijderen_BijNetwerkfout_GeeftMislukteResultaat()
    {
        var service = new LessonManagementService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var (success, message) = await service.DeleteLessonAsync(lessonId: 1);

        Assert.False(success);
        Assert.Contains("Netwerkfout", message);
    }

    // ── Lid toevoegen zonder credit ───────────────────────────────────────────

    [Fact]
    public async Task LidToevoegen_BijSucces_GeeftSuccesTerug()
    {
        var body = new ApiResult { Success = true, Message = "Lid succesvol toegevoegd." };
        var service = new LessonManagementService(MockHttpMessageHandler.CreateJsonClient(body));

        var (success, _) = await service.AddMemberToLessonAsync(lessonId: 1, userId: 42);

        Assert.True(success);
    }

    [Fact]
    public async Task LidToevoegen_LesVolOfAlAangemeld_GeeftMislukteResultaat()
    {
        var body = new ApiResult { Success = false, Message = "Dit lid is al aangemeld voor deze les." };
        var service = new LessonManagementService(MockHttpMessageHandler.CreateJsonClient(body));

        var (success, message) = await service.AddMemberToLessonAsync(lessonId: 1, userId: 42);

        Assert.False(success);
        Assert.False(string.IsNullOrEmpty(message));
    }

    // ── Dropdown lijsten ophalen ──────────────────────────────────────────────

    [Fact]
    public async Task GetWorkouts_RetourneertItems()
    {
        var body = new[] { new { id = 1, name = "Yoga", color = "#5B6636" } };
        var service = new LessonManagementService(MockHttpMessageHandler.CreateJsonClient(body));

        var result = (await service.GetWorkoutsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("Yoga", result[0].Name);
    }

    [Fact]
    public async Task GetWorkouts_BijNetwerkfout_RetourneertLegeReeks()
    {
        var service = new LessonManagementService(MockHttpMessageHandler.CreateNetworkErrorClient());

        var result = await service.GetWorkoutsAsync();

        Assert.Empty(result);
    }

    // ── Lescapaciteit: validatielogica ────────────────────────────────────────

    [Theory]
    [InlineData(0, 15, false)]   // 0/15 → niet vol
    [InlineData(14, 15, false)]  // 14/15 → niet vol
    [InlineData(15, 15, true)]   // 15/15 → vol
    [InlineData(16, 15, true)]   // 16/15 → overvol (edge case)
    public void LescapaciteitBereikt_DrempelLogica(int current, int max, bool expected)
        => Assert.Equal(expected, LessonCapacityRules.IsAtCapacity(current, max));
}

internal static class LessonCapacityRules
{
    public static bool IsAtCapacity(int current, int max) => current >= max;
}

// Minimal helper that wraps MockHttpMessageHandler for the Delete test
internal sealed class MockHttpMessageHandlerFactory
{
    private readonly Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> _factory;
    public MockHttpMessageHandlerFactory(
        Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> factory)
        => _factory = factory;
    public System.Net.Http.HttpClient CreateClient()
    {
        var handler = new FitLife.Tests.Fakes.MockHttpMessageHandler(_factory);
        return new System.Net.Http.HttpClient(handler)
            { BaseAddress = new Uri("http://localhost/") };
    }
}