namespace FitLife.UITests.Pages;

/// <summary>Page object for LoginPage — identified by AutomationId, language-independent.</summary>
public sealed class LoginPageObject(WindowsDriver driver)
{
    private const string LogoId     = "LoginLogo";
    private const string EmailId    = "LoginEmailEntry";
    private const string PasswordId = "LoginPasswordEntry";
    private const string ButtonId   = "LoginButton";
    private const string ErrorId    = "LoginErrorLabel";

    // ── Waits ─────────────────────────────────────────────────────────────────

    public void WaitForPage()
    {
        var wait = new WebDriverWait(driver, AppiumConfig.ElementTimeout);
        wait.Until(d => d.FindElement(MobileBy.AccessibilityId(LogoId)).Displayed);
    }

    // ── Element accessors ─────────────────────────────────────────────────────

    public AppiumElement Logo          => driver.FindElement(MobileBy.AccessibilityId(LogoId));
    public AppiumElement EmailEntry    => driver.FindElement(MobileBy.AccessibilityId(EmailId));
    public AppiumElement PasswordEntry => driver.FindElement(MobileBy.AccessibilityId(PasswordId));
    public AppiumElement LoginButton   => driver.FindElement(MobileBy.AccessibilityId(ButtonId));
    public AppiumElement ErrorLabel    => driver.FindElement(MobileBy.AccessibilityId(ErrorId));

    public bool IsErrorVisible()
    {
        try { return driver.FindElement(MobileBy.AccessibilityId(ErrorId)).Displayed; }
        catch (NoSuchElementException) { return false; }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    public void EnterEmail(string email)
    {
        var entry = EmailEntry;
        entry.Clear();
        entry.SendKeys(email);
    }

    public void EnterPassword(string password)
    {
        var entry = PasswordEntry;
        entry.Clear();
        entry.SendKeys(password);
    }

    public void TapLogin() => LoginButton.Click();
}
