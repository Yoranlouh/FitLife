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
    /// <summary>
    /// Absolute path to the compiled MAUI Windows exe.
    /// BaseDirectory is ...\FitLife.UITests\bin\Debug\net10.0\, so four "../" reach the
    /// repository root (net10.0 → Debug → bin → FitLife.UITests → repo root). The exe is
    /// named after the project (FitLife.Maui.exe) and lives in a runtime-identifier
    /// subfolder (e.g. win-x64), so we resolve it dynamically rather than hard-coding the RID.
    /// </summary>
    internal static readonly string AppExePath = ResolveAppExePath();

    private const string WindowsTfm = "net10.0-windows10.0.19041.0";
    private const string AppExeName = "FitLife.Maui.exe";

    private static string ResolveAppExePath()
    {
        var tfmDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "FitLife.Maui", "bin", "Debug", WindowsTfm));

        // Default (expected) location, also used in the "not found" message when missing.
        var expected = Path.Combine(tfmDir, "win-x64", AppExeName);

        if (Directory.Exists(tfmDir))
        {
            // The RID subfolder name can vary (win-x64 / win10-x64 / …); search for the exe.
            var match = Directory.GetFiles(tfmDir, AppExeName, SearchOption.AllDirectories)
                                 .FirstOrDefault();
            if (match is not null) return match;
        }

        return expected;
    }

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
