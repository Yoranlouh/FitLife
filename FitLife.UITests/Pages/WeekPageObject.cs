namespace FitLife.UITests.Pages;

/// <summary>Page object for WeekPage — identified by AutomationId, language-independent.</summary>
public sealed class WeekPageObject(WindowsDriver driver)
{
    private const string BackBtnId          = "WeekBackBtn";
    private const string RangeLabelId       = "WeekRangeLabel";
    private const string PrevBtnId          = "WeekPrevBtn";
    private const string NextBtnId          = "WeekNextBtn";
    private const string ListViewBtnId      = "WeekListViewBtn";
    private const string LoadingIndicatorId = "WeekLoadingIndicator";
    private const string EmptyMessageId     = "WeekEmptyMessage";

    // ── Waits / state checks ──────────────────────────────────────────────────

    public void WaitForPage()
    {
        var wait = new WebDriverWait(driver, AppiumConfig.ElementTimeout);
        wait.Until(d => d.FindElement(MobileBy.AccessibilityId(RangeLabelId)).Displayed);
    }

    public bool IsOnWeekPage()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(RangeLabelId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    public bool IsLoading()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(LoadingIndicatorId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    public bool IsEmptyMessageVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(EmptyMessageId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    // ── Element accessors ─────────────────────────────────────────────────────

    public AppiumElement BackButton     => driver.FindElement(MobileBy.AccessibilityId(BackBtnId));
    public AppiumElement WeekRangeLabel => driver.FindElement(MobileBy.AccessibilityId(RangeLabelId));
    public AppiumElement PrevButton     => driver.FindElement(MobileBy.AccessibilityId(PrevBtnId));
    public AppiumElement NextButton     => driver.FindElement(MobileBy.AccessibilityId(NextBtnId));
    public AppiumElement ListViewButton => driver.FindElement(MobileBy.AccessibilityId(ListViewBtnId));

    public string GetWeekRangeText() => WeekRangeLabel.Text;

    // ── Navigation actions ────────────────────────────────────────────────────

    public void TapPreviousWeek()
    {
        PrevButton.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
    }

    public void TapNextWeek()
    {
        NextButton.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
    }

    public void TapBack()
    {
        BackButton.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
    }

    public void TapListView() => ListViewButton.Click();

    public void WaitForLoadingComplete()
    {
        var wait = new WebDriverWait(driver, AppiumConfig.ElementTimeout);
        wait.Until(_ => !IsLoading());
    }
}
