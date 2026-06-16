using System.Globalization;

namespace FitLife.Tests.Fakes;

/// <summary>
/// Test-local implementation of the translation lookup logic.
/// Mirrors the key-lookup and fallback behaviour of the production Translator
/// without any MAUI dependency (no Preferences, no platform CultureInfo side effects).
/// Contains a representative subset of NL/EN keys sufficient to test all behaviour patterns.
/// </summary>
internal sealed class FakeTranslator
{
    private string _language = "nl";

    public string Language => _language;

    public void SetLanguage(string lang)
        => _language = lang == "en" ? "en" : "nl";

    /// <summary>Looks up a key in the active-language dictionary; returns the key itself when not found.</summary>
    public string Get(string key)
    {
        if (_language == "en" && En.TryGetValue(key, out var en)) return en;
        return Nl.TryGetValue(key, out var nl) ? nl : key;
    }

    /// <summary>Get with format-arg substitution, mirroring Translator.T(key, args).</summary>
    public string Get(string key, params object?[] args)
        => string.Format(CultureInfo.InvariantCulture, Get(key), args);

    // ── Dutch dictionary (representative subset) ──────────────────────────────

    private static readonly Dictionary<string, string> Nl = new()
    {
        ["Common_OK"]            = "OK",
        ["Common_Cancel"]        = "Annuleren",
        ["Common_Unlimited"]     = "Onbeperkt",
        ["Common_Error"]         = "Fout",
        ["Common_NotLoggedIn"]   = "Niet ingelogd",

        ["Login_Button"]         = "Inloggen",
        ["Login_EnterEmail"]     = "Voer je email adres in.",
        ["Login_EnterPassword"]  = "Voer je wachtwoord in.",

        ["Detail_Register"]      = "Aanmelden",
        ["Detail_Unregister"]    = "Afmelden",
        ["Detail_CreditsLeft"]   = "{0} credits over",
        ["Detail_LessonFullWaitlistTitle"] = "Les is vol",
        ["Detail_LessonFullWaitlistBody"]  = "De les is vol. Je bent automatisch toegevoegd aan de wachtlijst.",
        ["Detail_AlreadyOnWaitlist"]       = "Je staat al op de wachtlijst voor deze les.",
        ["Detail_WaitlistFailedTitle"]     = "Wachtlijst mislukt",
        ["Detail_WaitlistFailedBody"]      = "Je kon niet worden toegevoegd aan de wachtlijst. Probeer het later opnieuw.",

        ["Attendance_CheckInBtn"]   = "Inchecken via RFID",
        ["Attendance_SimulateGps"]  = "Simuleer aankomst bij locatie",
        ["Attendance_CheckedIn"]    = "Aanwezig geregistreerd ✓",
        ["Attendance_NotYetOpen"]   = "Inchecken beschikbaar vanaf {0:HH:mm}",
        ["Attendance_LessonEnded"]  = "De les is beëindigd",
        ["Attendance_AlertTitle"]   = "Ingecheckt!",
        ["Attendance_AlertBody"]    = "Je aanwezigheid bij {0} is geregistreerd.",
        ["Attendance_GpsAlertTitle"] = "Locatie gedetecteerd",
        ["Attendance_GpsAlertBody"]  = "Je aanwezigheid is automatisch geregistreerd via locatie.",

        ["Notif_ReservedTitle"]  = "Aangemeld voor les",
        ["Notif_ReservedBody"]   = "Je bent ingeschreven voor {0} op {1:d MMMM 'om' HH:mm}.",
        ["Notif_WaitlistTitle"]  = "Op de wachtlijst",
        ["Notif_CheckedInTitle"] = "Aanwezigheid geregistreerd",
        ["Notif_CheckedInBody"]  = "Je aanwezigheid bij {0} op {1:HH:mm} is geregistreerd.",

        ["Time_JustNow"]  = "Zojuist",
        ["Time_MinAgo"]   = "{0} min geleden",
        ["Time_HourAgo"]  = "{0} uur geleden",

        ["Subscription_CreditsPerMonth"]  = "{0} credits per maand",
        ["Subscription_UnlimitedCredits"] = "Onbeperkt credits",
    };

    // ── English dictionary (representative subset) ────────────────────────────

    private static readonly Dictionary<string, string> En = new()
    {
        ["Common_OK"]            = "OK",
        ["Common_Cancel"]        = "Cancel",
        ["Common_Unlimited"]     = "Unlimited",
        ["Common_Error"]         = "Error",
        ["Common_NotLoggedIn"]   = "Not logged in",

        ["Login_Button"]         = "Log in",
        ["Login_EnterEmail"]     = "Please enter your email address.",
        ["Login_EnterPassword"]  = "Please enter your password.",

        ["Detail_Register"]      = "Register",
        ["Detail_Unregister"]    = "Unregister",
        ["Detail_CreditsLeft"]   = "{0} credits left",
        ["Detail_LessonFullWaitlistTitle"] = "Lesson full",
        ["Detail_LessonFullWaitlistBody"]  = "The lesson is full. You have been automatically added to the waitlist.",
        ["Detail_AlreadyOnWaitlist"]       = "You are already on the waitlist for this lesson.",
        ["Detail_WaitlistFailedTitle"]     = "Waitlist failed",
        ["Detail_WaitlistFailedBody"]      = "You could not be added to the waitlist. Please try again later.",

        ["Attendance_CheckInBtn"]   = "Check in via RFID",
        ["Attendance_SimulateGps"]  = "Simulate arrival at location",
        ["Attendance_CheckedIn"]    = "Attendance registered ✓",
        ["Attendance_NotYetOpen"]   = "Check-in available from {0:HH:mm}",
        ["Attendance_LessonEnded"]  = "The lesson has ended",
        ["Attendance_AlertTitle"]   = "Checked in!",
        ["Attendance_AlertBody"]    = "Your attendance at {0} has been registered.",
        ["Attendance_GpsAlertTitle"] = "Location detected",
        ["Attendance_GpsAlertBody"]  = "Your attendance has been automatically registered via location.",

        ["Notif_ReservedTitle"]  = "Registered for lesson",
        ["Notif_CheckedInTitle"] = "Attendance registered",

        ["Time_JustNow"]  = "Just now",
        ["Time_MinAgo"]   = "{0} min ago",
        ["Time_HourAgo"]  = "{0} hour(s) ago",

        ["Subscription_CreditsPerMonth"]  = "{0} credits per month",
        ["Subscription_UnlimitedCredits"] = "Unlimited credits",
    };
}
