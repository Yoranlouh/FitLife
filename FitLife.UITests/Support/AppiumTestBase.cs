namespace FitLife.UITests.Support;

/// <summary>
/// Shared xUnit collection fixture that owns the WireMock stub server, the Appium
/// Windows driver, and the initial login flow. All UI test classes receive this
/// fixture via ICollectionFixture so the app launches once and stays open across
/// the full test run. Tests that need a clean state navigate back to the relevant
/// starting page using the page-object helpers.
/// </summary>
public sealed class AppiumFixture : IDisposable
{
    public WindowsDriver Driver { get; }
    private readonly MockApiServer _mockServer;

    public AppiumFixture()
    {
        // 0. Verify the prerequisites up front so failures give an actionable message
        //    instead of a raw SocketException ("connection refused") on every test.
        if (!File.Exists(AppiumConfig.AppExePath))
            throw new InvalidOperationException(
                $"De MAUI-app is niet gevonden op:\n  {AppiumConfig.AppExePath}\n" +
                "Bouw eerst de app:\n" +
                "  dotnet build FitLife.Maui -f net10.0-windows10.0.19041.0 -c Debug");

        // 1. Start the mock API before the app touches the network
        _mockServer = new MockApiServer();

        // 2. Launch the MAUI Windows app via the Appium Windows driver
        var options = new AppiumOptions();
        options.App            = AppiumConfig.AppExePath;
        options.PlatformName   = "Windows";
        options.AutomationName = "Windows";

        try
        {
            Driver = new WindowsDriver(
                AppiumConfig.AppiumServerUri,
                options,
                TimeSpan.FromMilliseconds(AppiumConfig.AppLaunchTimeoutMs));
        }
        catch (Exception ex) when (ex is WebDriverException or System.Net.Sockets.SocketException
                                   || ex.InnerException is System.Net.Sockets.SocketException)
        {
            _mockServer.Dispose();
            throw new InvalidOperationException(
                $"Kon geen verbinding maken met de Appium-server op {AppiumConfig.AppiumServerUri}.\n" +
                "De UI-tests vereisen een draaiende Appium-server met de Windows-driver. Start die eerst:\n" +
                "  1. npm install -g appium\n" +
                "  2. appium driver install windows\n" +
                "  3. appium            (laat dit in een aparte terminal draaien)\n" +
                "Zorg ook dat er geen echte FitLife.API op poort 5000 draait (WireMock bindt die poort).",
                ex);
        }

        Driver.Manage().Timeouts().ImplicitWait = AppiumConfig.ElementTimeout;

        // 3. Complete the login flow so tests start on the Home page
        PerformLogin();
    }

    // ── Login helper ──────────────────────────────────────────────────────────

    private void PerformLogin()
    {
        var login = new LoginPageObject(Driver);
        login.WaitForPage();
        login.EnterEmail(AppiumConfig.TestEmail);
        login.EnterPassword(AppiumConfig.TestPassword);
        login.TapLogin();
        // Wait until the Home page logo is visible (confirms successful login)
        new HomePageObject(Driver).WaitForPage();
    }

    /// <summary>
    /// Navigates back to the Home page from anywhere in the app by pressing the
    /// system Back button until the Home page logo is present, or by using the
    /// Shell flyout. Tests call this to reset to a known state.
    /// </summary>
    public void GoToHomePage(int maxBackPresses = 5)
    {
        var home = new HomePageObject(Driver);
        for (var i = 0; i < maxBackPresses; i++)
        {
            if (home.IsOnHomePage()) return;
            Driver.Navigate().Back();
            Thread.Sleep(AppiumConfig.InteractionDelay);
        }
    }

    public void Dispose()
    {
        try { Driver?.Quit(); } catch { /* ignore during teardown */ }
        _mockServer?.Dispose();
    }
}

/// <summary>
/// xUnit collection definition — ensures one shared AppiumFixture for all UI tests
/// and disables parallel execution (a single device can only run one Appium session).
/// </summary>
[CollectionDefinition(Name)]
public class UITestCollection : ICollectionFixture<AppiumFixture>
{
    public const string Name = "UITests";
}
