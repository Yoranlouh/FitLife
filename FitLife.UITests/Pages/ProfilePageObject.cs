namespace FitLife.UITests.Pages;

/// <summary>Page object for ProfilePage — identified by AutomationId, language-independent.</summary>
public sealed class ProfilePageObject(WindowsDriver driver)
{
    private const string BackBtnId                = "ProfileBackBtn";
    private const string FirstNameId              = "ProfileFirstName";
    private const string LastNameId               = "ProfileLastName";
    private const string SubscriptionNameId       = "ProfileSubscriptionName";
    private const string CreditsDisplayId         = "ProfileCreditsDisplay";
    private const string ChangePhotoBtnId         = "ProfileChangePhotoBtn";
    private const string ManageSubscriptionBtnId  = "ProfileManageSubscriptionBtn";
    private const string ViewMyLessonsBtnId       = "ProfileViewMyLessonsBtn";
    private const string LogoutBtnId              = "ProfileLogoutBtn";

    // ── Waits / state checks ──────────────────────────────────────────────────

    public void WaitForPage()
    {
        var wait = new WebDriverWait(driver, AppiumConfig.ElementTimeout);
        wait.Until(d => d.FindElement(MobileBy.AccessibilityId(FirstNameId)).Displayed);
    }

    public bool IsOnProfilePage()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(FirstNameId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    // ── Element accessors ─────────────────────────────────────────────────────

    public AppiumElement BackButton            => driver.FindElement(MobileBy.AccessibilityId(BackBtnId));
    public AppiumElement FirstName             => driver.FindElement(MobileBy.AccessibilityId(FirstNameId));
    public AppiumElement LastName              => driver.FindElement(MobileBy.AccessibilityId(LastNameId));
    public AppiumElement SubscriptionName      => driver.FindElement(MobileBy.AccessibilityId(SubscriptionNameId));
    public AppiumElement CreditsDisplay        => driver.FindElement(MobileBy.AccessibilityId(CreditsDisplayId));
    public AppiumElement ChangePhotoButton     => driver.FindElement(MobileBy.AccessibilityId(ChangePhotoBtnId));
    public AppiumElement ManageSubscriptionBtn => driver.FindElement(MobileBy.AccessibilityId(ManageSubscriptionBtnId));
    public AppiumElement ViewMyLessonsBtn      => driver.FindElement(MobileBy.AccessibilityId(ViewMyLessonsBtnId));
    public AppiumElement LogoutButton          => driver.FindElement(MobileBy.AccessibilityId(LogoutBtnId));

    public string GetFirstName()        => FirstName.Text;
    public string GetSubscriptionName() => SubscriptionName.Text;
    public string GetCreditsText()      => CreditsDisplay.Text;

    // ── Actions ───────────────────────────────────────────────────────────────

    public void TapBack()
    {
        BackButton.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
    }

    public void TapManageSubscription() => ManageSubscriptionBtn.Click();
    public void TapViewMyLessons()      => ViewMyLessonsBtn.Click();

    /// <summary>
    /// Taps Logout and confirms the dialog. Returns a LoginPageObject for the
    /// resulting page (the app navigates back to the login screen after logout).
    /// </summary>
    public LoginPageObject TapLogoutAndConfirm()
    {
        LogoutButton.Click();
        Thread.Sleep(AppiumConfig.InteractionDelay);
        // Confirm the alert dialog (Windows: click the first/primary button)
        try
        {
            var confirmBtn = new WebDriverWait(driver, TimeSpan.FromSeconds(5))
                .Until(d => d.FindElement(By.Name("Ja")));
            confirmBtn.Click();
        }
        catch
        {
            // Dialog text may be in English depending on the app language setting
            try
            {
                var confirmBtn = driver.FindElement(By.Name("Yes"));
                confirmBtn.Click();
            }
            catch { /* dialog may have been dismissed automatically */ }
        }
        Thread.Sleep(AppiumConfig.InteractionDelay);
        return new LoginPageObject(driver);
    }
}
