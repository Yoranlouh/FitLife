namespace FitLife.UITests.Pages;

/// <summary>Page object for HomePage — identified by AutomationId, language-independent.</summary>
public sealed class HomePageObject(WindowsDriver driver)
{
    private const string LogoId              = "HomeLogo";
    private const string WelcomeId           = "HomeWelcomeLabel";
    private const string HamburgerBtnId      = "HomeHamburgerBtn";
    private const string NotificationBellId  = "HomeNotificationBell";
    private const string MemberTilesGridId   = "HomeMemberTilesGrid";
    private const string TileScheduleId      = "HomeTileSchedule";
    private const string TileReservationsId  = "HomeTileReservations";
    private const string TileProfileId       = "HomeTileProfile";
    private const string TileSubscriptionId  = "HomeTileSubscription";

    // ── Waits / state checks ──────────────────────────────────────────────────

    public void WaitForPage()
    {
        var wait = new WebDriverWait(driver, AppiumConfig.ElementTimeout);
        wait.Until(d => d.FindElement(MobileBy.AccessibilityId(LogoId)).Displayed);
    }

    public bool IsOnHomePage()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(LogoId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    // ── Element accessors ─────────────────────────────────────────────────────

    public AppiumElement Logo             => driver.FindElement(MobileBy.AccessibilityId(LogoId));
    public AppiumElement WelcomeLabel     => driver.FindElement(MobileBy.AccessibilityId(WelcomeId));
    public AppiumElement HamburgerButton  => driver.FindElement(MobileBy.AccessibilityId(HamburgerBtnId));
    public AppiumElement NotificationBell => driver.FindElement(MobileBy.AccessibilityId(NotificationBellId));
    public AppiumElement MemberTilesGrid  => driver.FindElement(MobileBy.AccessibilityId(MemberTilesGridId));
    public AppiumElement TileSchedule     => driver.FindElement(MobileBy.AccessibilityId(TileScheduleId));
    public AppiumElement TileReservations => driver.FindElement(MobileBy.AccessibilityId(TileReservationsId));
    public AppiumElement TileProfile      => driver.FindElement(MobileBy.AccessibilityId(TileProfileId));
    public AppiumElement TileSubscription => driver.FindElement(MobileBy.AccessibilityId(TileSubscriptionId));

    public bool IsMemberTilesGridVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(MemberTilesGridId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    // ── Navigation actions ────────────────────────────────────────────────────

    /// <summary>Taps the schedule tile. Returns a WeekPageObject for the page that opens.</summary>
    public WeekPageObject TapScheduleTile()
    {
        TileSchedule.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
        return new WeekPageObject(driver);
    }

    /// <summary>Taps the profile tile. Returns a ProfilePageObject for the page that opens.</summary>
    public ProfilePageObject TapProfileTile()
    {
        TileProfile.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
        return new ProfilePageObject(driver);
    }

    /// <summary>Taps the reservations tile. Returns a page object for verification.</summary>
    public void TapReservationsTile() => TileReservations.Click();

    /// <summary>Taps the subscription tile.</summary>
    public void TapSubscriptionTile() => TileSubscription.Click();

    /// <summary>Taps the hamburger menu button to open the flyout.</summary>
    public void TapHamburger() => HamburgerButton.Click();

    /// <summary>Taps the notification bell.</summary>
    public void TapNotificationBell() => NotificationBell.Click();
}
