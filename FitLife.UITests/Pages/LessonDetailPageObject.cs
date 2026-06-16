namespace FitLife.UITests.Pages;

/// <summary>Page object for LessonDetailPage — identified by AutomationId, language-independent.</summary>
public sealed class LessonDetailPageObject(WindowsDriver driver)
{
    private const string BackBtnId        = "DetailBackBtn";
    private const string WorkoutNameId    = "DetailWorkoutName";
    private const string LocationId       = "DetailLocation";
    private const string ReserveBtnId     = "DetailReserveBtn";
    private const string UnregisterBtnId  = "DetailUnregisterBtn";
    private const string ExpiredBtnId     = "DetailExpiredBtn";
    private const string CheckInBtnId     = "DetailCheckInBtn";
    private const string CheckedInBadgeId = "DetailCheckedInBadge";
    private const string ParticipantsId   = "DetailParticipants";

    // ── Waits / state checks ──────────────────────────────────────────────────

    public void WaitForPage()
    {
        var wait = new WebDriverWait(driver, AppiumConfig.ElementTimeout);
        wait.Until(d => d.FindElement(MobileBy.AccessibilityId(WorkoutNameId)).Displayed);
    }

    public bool IsReserveBtnVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(ReserveBtnId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    public bool IsUnregisterBtnVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(UnregisterBtnId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    public bool IsExpiredBtnVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(ExpiredBtnId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    public bool IsCheckedInBadgeVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(CheckedInBadgeId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    public bool IsCheckInBtnVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(CheckInBtnId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    // ── Element accessors ─────────────────────────────────────────────────────

    public AppiumElement BackButton      => driver.FindElement(MobileBy.AccessibilityId(BackBtnId));
    public AppiumElement WorkoutName     => driver.FindElement(MobileBy.AccessibilityId(WorkoutNameId));
    public AppiumElement Location        => driver.FindElement(MobileBy.AccessibilityId(LocationId));
    public AppiumElement ParticipantsRow => driver.FindElement(MobileBy.AccessibilityId(ParticipantsId));

    public string GetWorkoutNameText() => WorkoutName.Text;
    public string GetLocationText()    => Location.Text;

    // ── Actions ───────────────────────────────────────────────────────────────

    public void TapBack()
    {
        BackButton.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
    }

    public void TapReserve()   => driver.FindElement(MobileBy.AccessibilityId(ReserveBtnId)).Click();
    public void TapUnregister() => driver.FindElement(MobileBy.AccessibilityId(UnregisterBtnId)).Click();
}
