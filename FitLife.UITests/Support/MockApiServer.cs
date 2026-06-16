using WireMock.Server;
using WireMock.Settings;
using WireMock.RequestBuilders;
using WireMockResponse = WireMock.ResponseBuilders.Response;

namespace FitLife.UITests.Support;

/// <summary>
/// Starts a WireMock HTTP stub server on port 5000 (the app's hard-coded API base URL).
/// Provides deterministic, network-independent responses for every endpoint the app calls.
/// </summary>
internal sealed class MockApiServer : IDisposable
{
    private readonly WireMockServer _server;

    internal MockApiServer()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Port        = AppiumConfig.MockApiPort,
            StartAdminInterface = false
        });

        RegisterEndpoints();
    }

    private void RegisterEndpoints()
    {
        // ── Authentication ──────────────────────────────────────────────────
        _server.Given(
            Request.Create().WithPath("/auth/login").UsingPost()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    success              = true,
                    userId               = AppiumConfig.TestUserId,
                    displayName          = AppiumConfig.TestName,
                    role                 = AppiumConfig.TestRole,
                    credits              = 9,
                    subscriptionType     = "Rookie",
                    subscriptionRenewalDate = "2026-12-01",
                    photoUrl             = (string?)null,
                    email                = AppiumConfig.TestEmail
                })
        );

        // ── Notifications ───────────────────────────────────────────────────
        _server.Given(
            Request.Create().WithPath($"/users/{AppiumConfig.TestUserId}/notifications").UsingGet()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(Array.Empty<object>())
        );

        _server.Given(
            Request.Create().WithPath($"/users/{AppiumConfig.TestUserId}/notifications/mark-all-read").UsingPut()
        ).RespondWith(
            WireMockResponse.Create().WithStatusCode(200)
        );

        _server.Given(
            Request.Create().WithPath($"/users/{AppiumConfig.TestUserId}/notifications").UsingPost()
        ).RespondWith(
            WireMockResponse.Create().WithStatusCode(201)
        );

        // ── Lessons ─────────────────────────────────────────────────────────
        _server.Given(
            Request.Create().WithPath("/lessons").UsingGet()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new[]
                {
                    new
                    {
                        id             = 101,
                        workoutName    = "Yoga",
                        instructorName = "Jan de Trainer",
                        locationName   = "Zaal A",
                        startTime      = DateTime.Today.AddDays(1).AddHours(9).ToString("o"),
                        endTime        = DateTime.Today.AddDays(1).AddHours(10).ToString("o"),
                        maxParticipants = 15,
                        participantCount = 3,
                        isBooked       = false,
                        workoutColor   = "#5B6636"
                    },
                    new
                    {
                        id             = 102,
                        workoutName    = "Spinning",
                        instructorName = "Marie Fietst",
                        locationName   = "Fietsenzaal",
                        startTime      = DateTime.Today.AddDays(2).AddHours(10).ToString("o"),
                        endTime        = DateTime.Today.AddDays(2).AddHours(11).ToString("o"),
                        maxParticipants = 20,
                        participantCount = 10,
                        isBooked       = true,
                        workoutColor   = "#5B6636"
                    }
                })
        );

        // ── Subscription status ─────────────────────────────────────────────
        _server.Given(
            Request.Create().WithPath($"/subscriptions/status/{AppiumConfig.TestUserId}").UsingGet()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    subscriptionType    = "Rookie",
                    credits             = 9,
                    renewalDate         = "2026-12-01",
                    hasPendingChange    = false,
                    pendingChangeInfo   = (string?)null
                })
        );

        _server.Given(
            Request.Create().WithPath("/subscriptions/plans").UsingGet()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(Array.Empty<object>())
        );

        // ── User lessons (my reservations) ──────────────────────────────────
        _server.Given(
            Request.Create().WithPath($"/users/{AppiumConfig.TestUserId}/lessons").UsingGet()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(Array.Empty<object>())
        );

        // ── Participants ────────────────────────────────────────────────────
        _server.Given(
            Request.Create().WithPath("/lessons/*/participants").UsingGet()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(Array.Empty<object>())
        );

        // ── Fallback: any unmatched request returns empty JSON ──────────────
        _server.Given(
            Request.Create().UsingAnyMethod()
        ).RespondWith(
            WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}")
        );
    }

    public void Dispose() => _server.Stop();
}
