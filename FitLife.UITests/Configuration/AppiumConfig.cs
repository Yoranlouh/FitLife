namespace FitLife.UITests.Configuration;

/// <summary>
/// Central configuration for Appium Windows driver and WireMock stub server.
///
/// Prerequisites before running:
///   1. npm install -g appium
///   2. appium driver install windows
///   3. appium                          (keep running in a separate terminal)
///   4. Build the MAUI app:
///      dotnet build ..\FitLife.Maui -f net10.0-windows10.0.19041.0 -c Debug
///   5. Ensure no real FitLife.API is running on port 5000 (WireMock will bind it).
/// </summary>
internal static class AppiumConfig
{
    /// <summary>Relative path from the test output directory to the compiled MAUI Windows exe.</summary>
    internal static readonly string AppExePath = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            @"..\..\..\..\..\FitLife.Maui\bin\Debug\net10.0-windows10.0.19041.0\FitLife.exe"));

    /// <summary>Appium server URI. Start with: appium</summary>
    internal static readonly Uri AppiumServerUri = new("http://127.0.0.1:4723");

    /// <summary>Port on which WireMock serves mock API responses (same as the app's hardcoded API port).</summary>
    internal const int MockApiPort = 5000;

    /// <summary>Milliseconds to wait for the MAUI app window to appear after launch.</summary>
    internal const int AppLaunchTimeoutMs = 15_000;

    /// <summary>Default timeout for finding UI elements and waiting for state transitions.</summary>
    internal static readonly TimeSpan ElementTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Short delay between UI interactions to allow animations to complete.</summary>
    internal static readonly TimeSpan InteractionDelay = TimeSpan.FromMilliseconds(300);

    // ── Test account used for all login flows ──────────────────────────────────

    internal const string TestEmail    = "test@fitlife.nl";
    internal const string TestPassword = "test123";
    internal const int    TestUserId   = 1;
    internal const string TestName     = "Test Gebruiker";
    internal const string TestRole     = "member";
}
