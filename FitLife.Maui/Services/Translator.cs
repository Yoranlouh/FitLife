using System.ComponentModel;
using System.Globalization;

namespace FitLife.Maui.Services;

// Central runtime translation service.
// All static UI text lives in the Nl/En dictionaries below. XAML binds to the
// indexer via the {helpers:Translate Key} markup extension; when the language
// changes, a single PropertyChanged(null) refreshes every bound label at once,
// so the whole UI switches instantly without restarting the app.
public sealed class Translator : INotifyPropertyChanged
{
    public static Translator Instance { get; } = new();

    // Same preference key the SettingsViewModel uses, so both stay in sync.
    private const string PreferenceKey = "settings_language";

    private string _language = "nl";
    public string Language => _language;

    public event PropertyChangedEventHandler? PropertyChanged;

    private Translator() { }

    // Called once at startup (App constructor) to restore the saved language
    // before any page is built.
    public static void Initialize()
    {
        Instance.Apply(Preferences.Get(PreferenceKey, "nl"));
    }

    // Switches the UI language at runtime and persists the choice.
    public void SetLanguage(string lang)
    {
        Apply(lang);
        Preferences.Set(PreferenceKey, _language);
    }

    private void Apply(string lang)
    {
        _language = lang == "en" ? "en" : "nl";

        // Also switch the culture so date/number StringFormats ("dddd d MMMM")
        // render in the chosen language when pages reload their data.
        var culture = new CultureInfo(_language == "en" ? "en-US" : "nl-NL");
        CultureInfo.DefaultThreadCurrentCulture   = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture   = culture;
        CultureInfo.CurrentUICulture = culture;

        // Property name null/empty = "everything changed" → all indexer bindings refresh.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    // Indexer used by XAML bindings: {Binding [Key], Source=Translator.Instance}
    public string this[string key] => Get(key);

    // Static helper for code-behind / ViewModels.
    public static string T(string key) => Instance.Get(key);

    // Format overload for interpolated messages, e.g. T("Detail_Expires", date).
    // Uses the active culture (set in Apply) so dates/numbers render correctly.
    public static string T(string key, params object?[] args)
        => string.Format(CultureInfo.CurrentCulture, Instance.Get(key), args);

    private string Get(string key)
    {
        if (_language == "en" && En.TryGetValue(key, out var en))
            return en;
        return Nl.TryGetValue(key, out var nl) ? nl : key;
    }

    private static readonly Dictionary<string, string> Nl = new()
    {
        // ── Common ──
        ["Common_Cancel"]   = "Annuleren",
        ["Common_Yes"]      = "Ja",
        ["Common_No"]       = "Nee",
        ["Common_Error"]    = "Fout",
        ["Common_Success"]  = "Gelukt",

        // ── Login ──
        ["Login_Subtitle"]            = "Log in om verder te gaan",
        ["Login_EmailPlaceholder"]    = "Email adres",
        ["Login_PasswordPlaceholder"] = "Wachtwoord",
        ["Login_Button"]              = "Inloggen",
        ["Login_Footer"]              = "Lid worden? Meld je aan via de website.",

        // ── Home ──
        ["Home_Title"]           = "Hoofdmenu",
        ["Home_Schedule"]        = "Rooster",
        ["Home_Reservations"]    = "Afspraken & reserveringen",
        ["Home_Profile"]         = "Profiel",
        ["Home_Subscription"]    = "Abonnement",
        ["Home_TrainerSchedule"] = "Trainer Rooster",
        ["Home_MyLessons"]       = "Mijn Lessen",
        ["Home_CreateLesson"]    = "Les Aanmaken",
        ["Home_ManageLessons"]   = "Lessen Beheer",
        ["Home_Instructors"]     = "Instructeurs",
        ["Home_Members"]         = "Leden",
        ["Home_WelcomeAdmin"]    = "Welkom, Admin",
        ["Home_WelcomeTrainer"]  = "Welkom, Trainer",
        ["Home_WelcomeMember"]   = "Welkom bij FitLife",

        // ── Flyout menu ──
        ["Menu_Home"]         = "Home",
        ["Menu_Settings"]     = "Instellingen",
        ["Menu_MySportclub"]  = "Mijn sportclub",
        ["Menu_Logout"]       = "Uitloggen",
        ["Menu_MyAccount"]    = "Mijn account",
        ["Menu_Reservations"] = "Afspraken & reserveringen",
        ["Menu_Schedule"]     = "Rooster",
        ["Role_Admin"]        = "Administrator",
        ["Role_Instructor"]   = "Instructeur",
        ["Role_Member"]       = "Lid",
        ["Logout_Title"]      = "Uitloggen",
        ["Logout_Confirm"]    = "Weet je zeker dat je wilt uitloggen?",
        ["Logout_Yes"]        = "Ja, uitloggen",

        // ── Settings ──
        ["Settings_Title"]               = "Instellingen",
        ["Settings_Language"]            = "TAAL",
        ["Settings_Notifications"]       = "NOTIFICATIES",
        ["Settings_Waitlist"]            = "Wachtlijst",
        ["Settings_WaitlistDesc"]        = "Melding als er een plek vrijkomt",
        ["Settings_Push"]                = "Pushmeldingen",
        ["Settings_PushDesc"]            = "Alle meldingen van de app",
        ["Settings_Cancelled"]           = "Afgelaste trainingen",
        ["Settings_CancelledDesc"]       = "Melding bij annulering van een les",
        ["Settings_News"]                = "Algemeen & nieuws",
        ["Settings_NewsDesc"]            = "Updates en aankondigingen van FitLife",
        ["Settings_Location"]            = "LOCATIE",
        ["Settings_LocationSharing"]     = "Locatie delen",
        ["Settings_LocationSharingDesc"] = "Toestemming voor automatisch inchecken",
        ["Settings_LocationInfo"]        = "FitLife vraagt eenmalig toestemming aan je apparaat zodra je dit aanzet. Je kunt dit altijd wijzigen via de app-instellingen van je telefoon.",

        // ── Mijn sportclub ──
        ["Sportclub_Title"]      = "Mijn Sportclub",
        ["Sportclub_Directions"] = "Routebeschrijving",

        // ── Profiel ──
        ["Profile_Title"]              = "Mijn Profiel",
        ["Profile_ChangePhoto"]        = "Foto wijzigen",
        ["Profile_DeletePhoto"]        = "Foto verwijderen",
        ["Profile_CreditEqualsLesson"] = "1 credit = 1 les",
        ["Profile_Credits"]            = "credits",
        ["Profile_MySubscription"]     = "Mijn Abonnement",
        ["Profile_Type"]               = "Type:",
        ["Profile_RemainingCredits"]   = "Resterende credits deze maand:",
        ["Profile_Renewal"]            = "Vernieuwing:",
        ["Profile_ManageSubscription"] = "Abonnement Beheren",
        ["Profile_ViewMyLessons"]      = "Mijn Lessen Bekijken",
        ["Profile_NoSubscription"]     = "Geen abonnement",

        // ── Notificaties ──
        ["Notifications_Title"]     = "Notificaties",
        ["Notifications_AllRead"]   = "Alles gelezen",
        ["Notifications_Empty"]     = "Geen notificaties",
        ["Notifications_EmptyDesc"] = "Hier zie je je meldingen zodra ze binnenkomen.",

        // ── Abonnement ──
        ["Subscription_Title"]            = "Abonnement",
        ["Subscription_Current"]          = "Huidig Abonnement",
        ["Subscription_ManageCurrent"]    = "⚙️ Beheer Huidig Abonnement",
        ["Subscription_ChangeBilling"]    = "Wijzig factureringsperiode",
        ["Subscription_Cancel"]           = "Abonnement stopzetten",
        ["Subscription_ChangesNote"]      = "💡 Wijzigingen worden toegepast op je vernieuwingsdatum",
        ["Subscription_ViewPrices"]       = "Bekijk Prijzen",
        ["Subscription_Monthly"]          = "Maandelijks",
        ["Subscription_Yearly"]           = "Jaarlijks",
        ["Subscription_Save15"]           = "Bespaar ~15%",
        ["Subscription_Choose"]           = "Kies een Abonnement",
        ["Subscription_PerMonth"]         = " per maand",
        ["Subscription_CreditEqualsLesson"] = "1 credit = 1 les",
        ["Subscription_AccessAll"]        = "Toegang tot alle lessen",
        ["Subscription_OnlineReserve"]    = "Online reserveren via app",
        ["Subscription_CreditsRoll"]      = "Credits rollen over (max 1 maand)",
        ["Subscription_FreeDrink"]        = "Gratis sportdrank",
        ["Subscription_WaitlistPriority"] = "Prioriteit bij wachtlijsten",
        ["Subscription_GuestMonthly"]     = "1x per maand gastlid",
        ["Subscription_SelectThis"]       = "Kies dit abonnement",
        ["Subscription_HowTitle"]         = "ℹ️ Hoe werken abonnementswijzigingen?",
        ["Subscription_HowText"]          = "• Je kunt op elk moment van abonnement wisselen\n• De wijziging wordt toegepast op je volgende vervaldatum\n• Je blijft je huidige abonnement gebruiken tot die datum\n• Credits worden automatisch aangepast bij verlenging",
        ["Subscription_NoExpiry"]         = "Geen vervaldatum",

        // ── Mijn lessen ──
        ["MyLessons_Title"]         = "Mijn Lessen",
        ["MyLessons_HistoryTab"]    = "Geschiedenis",
        ["MyLessons_EmptyTitle"]    = "Geen aankomende lessen",
        ["MyLessons_EmptyDesc"]     = "Reserveer een les via het rooster om hem hier te zien.",
        ["MyLessons_Cancel"]        = "Annuleren",
        ["MyLessons_Total"]         = "Totaal",
        ["MyLessons_ThisMonth"]     = "Deze maand",
        ["MyLessons_ThisYear"]      = "Dit jaar",
        ["MyLessons_MostVisited"]   = "Meest bezochte les",
        ["MyLessons_Attended"]      = "Bezochte lessen",
        ["MyLessons_NoneAttended"]  = "Nog geen lessen bijgewoond",

        // ── Week/rooster ──
        ["Week_CalendarView"] = "Kalenderweergave",
        ["Week_ListView"]     = "Lijstweergave",

        // ── Dag ──
        ["Day_Title"]   = "Dag Overzicht",
        ["Day_Refresh"] = "Vernieuwen",

        // ── Lesaanbod ──
        ["Lessons_NoneFound"] = "Geen lessen gevonden",
        ["Lessons_Refresh"]   = "Vernieuwen",

        // ── Lesdetail ──
        ["Detail_Register"]       = "Aanmelden",
        ["Detail_RegisterClosed"] = "Aanmelden niet meer mogelijk",
        ["Detail_Unregister"]     = "Afmelden",
        ["Detail_CancelDeadline"] = "Afmelden is niet meer mogelijk (minder dan 1 uur voor aanvang).",
        ["Detail_Trainer"]        = "Trainer",
        ["Detail_PaymentMethod"]  = "Betaalmethode",
        ["Detail_EndTime"]        = "Eindtijd",
        ["Detail_Participants"]   = "Deelnemers",
        ["Detail_AdditionalInfo"] = "Aanvullende informatie",
        ["Detail_CancelInfo"]     = "Je kunt je afmelden tot 1 uur voor aanvang van de les.",
        ["Detail_ReserveInfo"]    = "Reserveren is mogelijk tot maximaal 1 week van tevoren.",

        // ── Deelnemers ──
        ["Participants_Title"]    = "Deelnemers",
        ["Participants_Buddies"]  = "Buddies",
        ["Participants_Waitlist"] = "Wachtlijst",

        // ── Trainer rooster ──
        ["Instructor_Title"]     = "Trainer Rooster",
        ["Instructor_NewLesson"] = "+ Nieuwe Les",
        ["Instructor_Edit"]      = "Wijzigen",
        ["Instructor_Delete"]    = "Verwijderen",

        // ── Les beheren ──
        ["Manage_Loading"]         = "Gegevens laden...",
        ["Manage_WorkoutType"]     = "WORKOUT TYPE",
        ["Manage_Date"]            = "DATUM",
        ["Manage_StartTime"]       = "BEGINTIJD",
        ["Manage_EndTime"]         = "EINDTIJD",
        ["Manage_MaxParticipants"] = "MAX. DEELNEMERS",
        ["Manage_MaxPlaceholder"]  = "bijv. 15",
        ["Manage_Location"]        = "LOCATIE / ZAAL",
        ["Manage_Instructor"]      = "INSTRUCTEUR",
        ["Manage_AddMember"]       = "LID TOEVOEGEN AAN LES",
        ["Manage_UserId"]          = "Gebruikers-ID",
        ["Manage_Add"]             = "Toevoegen",

        // ── Legenda ──
        ["Legend_Registered"] = "Aangemeld",
        ["Legend_Waitlist"]   = "Op wachtlijst",

        // ── Algemene meldingen / knoppen ──
        ["Common_OK"]          = "OK",
        ["Common_Info"]        = "Info",
        ["Common_Unlimited"]   = "Onbeperkt",
        ["Common_Unknown"]     = "Onbekend",
        ["Common_NotLoggedIn"] = "Niet ingelogd",
        ["Common_LoginAgain"]  = "Log opnieuw in.",
        ["Common_ErrorRetry"]  = "Er is een fout opgetreden. Probeer het later opnieuw.",
        ["Common_Loading"]     = "Laden...",

        // ── Login (validatie) ──
        ["Login_EnterEmail"]    = "Voer je email adres in.",
        ["Login_EnterPassword"] = "Voer je wachtwoord in.",
        ["Login_GenericError"]  = "Er is een fout opgetreden. Probeer het opnieuw.",

        // ── Lesdetail (meldingen) ──
        ["Detail_PageTitle"]            = "Groepsevent",
        ["Detail_CreditsLeft"]          = "{0} credits over",
        ["Detail_SubLineUnlimited"]     = "{0} · Onbeperkt lessen",
        ["Detail_SubLineCredit"]        = "{0} · 1 credit per les",
        ["Detail_Expires"]              = "Verloopt: {0:d MMMM yyyy}",
        ["Detail_BookableFrom"]         = "Reserveren mogelijk vanaf {0:d MMMM}",
        ["Detail_NotPossibleTitle"]     = "Niet mogelijk",
        ["Detail_TooFarBody"]           = "Je kunt maximaal 1 week van tevoren reserveren.\n{0}.",
        ["Detail_LoginToReserve"]       = "Log opnieuw in om een les te reserveren.",
        ["Detail_ReservedDefault"]      = "Je bent ingeschreven voor deze les.",
        ["Detail_ReservedAlertTitle"]   = "Ingeschreven!",
        ["Detail_ReserveFailedTitle"]   = "Inschrijven mislukt",
        ["Detail_ReserveFailedBody"]    = "Je kon niet worden ingeschreven.",
        ["Detail_CancelNotPossibleTitle"] = "Afmelden niet mogelijk",
        ["Detail_CancelDeadlineBody"]   = "Je kunt je niet meer afmelden. Afmelden is alleen mogelijk tot 1 uur voor aanvang van de les.",
        ["Detail_LoginToCancel"]        = "Je bent niet meer ingelogd. Log opnieuw in om je af te melden.",
        ["Detail_UnregisteredTitle"]    = "Afgemeld",
        ["Detail_UnregisteredBody"]     = "Je hebt je succesvol afgemeld.",
        ["Detail_CancelFailedTitle"]    = "Afmelden mislukt",
        ["Detail_CancelFailedBody"]     = "Je kon niet worden afgemeld.",
        ["Detail_OnWaitlistNow"]        = "Je staat nu op de wachtlijst.",
        ["Detail_LessonFullWaitlistTitle"] = "Les is vol",
        ["Detail_LessonFullWaitlistBody"]  = "De les is vol. Je bent automatisch toegevoegd aan de wachtlijst.",
        ["Detail_AlreadyOnWaitlist"]       = "Je staat al op de wachtlijst voor deze les.",
        ["Detail_WaitlistFailedTitle"]     = "Wachtlijst mislukt",
        ["Detail_WaitlistFailedBody"]      = "Je kon niet worden toegevoegd aan de wachtlijst. Probeer het later opnieuw.",

        // ── Notificatie-inhoud ──
        ["Notif_ReservedTitle"]    = "Aangemeld voor les",
        ["Notif_ReservedBody"]     = "Je bent ingeschreven voor {0} op {1:d MMMM 'om' HH:mm}.",
        ["Notif_WaitlistTitle"]    = "Op de wachtlijst",
        ["Notif_WaitlistBody"]     = "Je staat op de wachtlijst voor {0} op {1:d MMMM 'om' HH:mm}. We laten je weten als er een plek vrijkomt.",
        ["Notif_ExpiringTitle"]    = "Abonnement verloopt bijna",
        ["Notif_ExpiringBody"]     = "Je {0} abonnement verloopt op {1:d MMMM yyyy}. Verleng op tijd om te blijven sporten.",

        // ── Notificatie-tijd ──
        ["Time_JustNow"]   = "Zojuist",
        ["Time_MinAgo"]    = "{0} min geleden",
        ["Time_HourAgo"]   = "{0} uur geleden",

        // ── Mijn lessen (meldingen) ──
        ["MyLessons_ConfirmTitle"]  = "Bevestigen",
        ["MyLessons_ConfirmBody"]   = "Weet je zeker dat je de reservering voor {0} wilt annuleren?",
        ["MyLessons_LoginToCancel"] = "Je bent niet meer ingelogd. Log opnieuw in om je reservering te annuleren.",
        ["MyLessons_CancelledTitle"]= "Geannuleerd",
        ["MyLessons_CancelledBody"] = "Je reservering is geannuleerd.",
        ["MyLessons_CancelFailedTitle"] = "Annuleren mislukt",
        ["MyLessons_CancelFailedBody"]  = "De reservering kon niet worden geannuleerd.",

        // ── Profiel (meldingen) ──
        ["Profile_PhotoUpdated"]      = "Profielfoto bijgewerkt.",
        ["Profile_PhotoUploadFailed"] = "Foto uploaden mislukt. Controleer je verbinding.",
        ["Profile_DeletePhotoTitle"]  = "Foto verwijderen",
        ["Profile_DeletePhotoBody"]   = "Weet je zeker dat je je profielfoto wilt verwijderen?",
        ["Profile_DeleteBtn"]         = "Verwijderen",
        ["Profile_PhotoDeleteFailed"] = "Foto verwijderen mislukt. Controleer je verbinding.",
        ["Profile_PendingChange"]     = "Wijzigt naar {0} ({1}) op {2:dd-MM-yyyy}",

        // ── Abonnement (meldingen + display) ──
        ["Subscription_NotLoggedInTitle"]  = "Niet ingelogd",
        ["Subscription_ExpiresOn"]         = "Verloopt op: {0:dd-MM-yyyy}",
        ["Subscription_PendingMessage"]    = "Je abonnement wordt gewijzigd naar {0} ({1}) op {2:dd-MM-yyyy}",
        ["Subscription_MustLoginChange"]   = "Je moet ingelogd zijn om je abonnement te wijzigen.",
        ["Subscription_AlreadyHave"]       = "Je hebt al dit abonnement.",
        ["Subscription_ChangeTitle"]       = "Abonnement wijzigen",
        ["Subscription_ChangeConfirm"]     = "Weet je zeker dat je wilt overstappen naar {0} ({1}, €{2:F0})?\n\nDe wijziging wordt toegepast op {3:dd-MM-yyyy}.",
        ["Subscription_YesChange"]         = "Ja, wijzigen",
        ["Subscription_ChangeSuccess"]     = "{0}\n\nHet eventuele resterende saldo van je huidige abonnement wordt automatisch verrekend bij de eerstvolgende automatische incasso.",
        ["Subscription_SuccessTitle"]      = "Gelukt!",
        ["Subscription_MustLoginCancel"]   = "Je moet ingelogd zijn om je abonnement te annuleren.",
        ["Subscription_NoActive"]          = "Je hebt geen actief abonnement.",
        ["Subscription_CancelTitle"]       = "Abonnement stopzetten",
        ["Subscription_CancelConfirm"]     = "Weet je zeker dat je je {0} abonnement wilt stopzetten?\n\nJe abonnement blijft actief tot {1:dd-MM-yyyy}. Na deze datum wordt het niet verlengd.",
        ["Subscription_YesCancel"]         = "Ja, stopzetten",
        ["Subscription_MustLoginBilling"]  = "Je moet ingelogd zijn om je factureringsperiode te wijzigen.",
        ["Subscription_BillingTitle"]      = "Factureringsperiode wijzigen",
        ["Subscription_BillingConfirm"]    = "Weet je zeker dat je wilt overstappen van {0} naar {1} betalen?\n\nJe blijft je huidige {2} abonnement behouden.\n\nDe wijziging wordt toegepast op {3:dd-MM-yyyy}.",
        ["Subscription_Loading"]           = "Laden...",
        ["Billing_Yearly"]                 = "jaarlijks",
        ["Billing_Monthly"]                = "maandelijks",
        ["Subscription_UnlimitedCredits"]  = "Onbeperkt credits",
        ["Subscription_CreditsPerMonth"]   = "{0} credits per maand",
        ["Subscription_UnlimitedLessons"]  = "Onbeperkt lessen per maand",
        ["Subscription_LessonsPerMonth"]   = "{0} lessen per maand",

        // ── Trainer lessen (meldingen) ──
        ["Instructor_Empty"]          = "Je hebt geen geplande lessen als trainer.",
        ["Instructor_DeleteTitle"]    = "Verwijderen",
        ["Instructor_DeleteConfirm"]  = "Weet je zeker dat je '{0}' op {1:dd-MM-yyyy HH:mm} wilt verwijderen?",
        ["Instructor_YesDelete"]      = "Ja, verwijderen",
        ["Instructor_DeletedTitle"]   = "Verwijderd",

        // ── Les beheren (meldingen) ──
        ["Manage_LoadFailed"]        = "⚠️ Kon gegevens niet laden. Controleer de API-verbinding.",
        ["Manage_LoadError"]         = "⚠️ Fout bij laden: {0}",
        ["Manage_WorkoutsLoading"]   = "Workouts worden geladen...",
        ["Manage_ChooseWorkout"]     = "Kies een workout",
        ["Manage_LocationsLoading"]  = "Locaties worden geladen...",
        ["Manage_ChooseLocation"]    = "Kies een locatie",
        ["Manage_InstructorsLoading"]= "Instructeurs worden geladen...",
        ["Manage_ChooseInstructor"]  = "Kies een instructeur",
        ["Manage_SelectWorkout"]     = "Selecteer een workout type.",
        ["Manage_SelectLocation"]    = "Selecteer een locatie.",
        ["Manage_SelectInstructor"]  = "Selecteer een instructeur.",
        ["Manage_EndAfterStart"]     = "Eindtijd moet na de begintijd liggen.",
        ["Manage_UpdatedTitle"]      = "Bijgewerkt",
        ["Manage_CreatedTitle"]      = "Les aangemaakt",
        ["Manage_SaveFirst"]         = "Sla de les eerst op voordat je leden toevoegt.",
        ["Manage_InvalidUserId"]     = "Voer een geldig gebruikers-ID in.",
        ["Manage_CreateTitle"]       = "Les Aanmaken",
        ["Manage_EditTitle"]         = "Les Wijzigen",
        ["Manage_SaveCreate"]        = "Les Aanmaken",
        ["Manage_SaveUpdate"]        = "Les Opslaan",
        ["Manage_PickWorkout"]       = "Selecteer workout...",
        ["Manage_PickLocation"]      = "Selecteer locatie...",
        ["Manage_PickInstructor"]    = "Selecteer instructeur...",

        // ── Deelnemers (display) ──
        ["Participants_Stats"]       = "{0}/{1} Deelnemers",

        // ── Week / rooster (display) ──
        ["Week_RangeText"]           = "Week {0}, {1:MMMM yyyy}",

        // ── Spinning fiets selectie ──
        ["Spinning_SelectBikeTitle"] = "Kies je fiets",
        ["Spinning_Row"]             = "Rij",
        ["Spinning_Bike"]            = "Fiets",
        ["Spinning_BikeLabel"]       = "Rij {0} - Fiets {1}",
        ["Spinning_BikeTaken"]       = "Bezet",
        ["Spinning_YourBike"]        = "Jouw fiets: {0}",
        ["Spinning_LegendFree"]      = "Vrij",
        ["Spinning_LegendYours"]     = "Jouw fiets",
        ["Spinning_LegendTaken"]     = "Bezet",

        // ── Aanwezigheidsregistratie ──
        ["Attendance_SectionTitle"]  = "AANWEZIGHEID",
        ["Attendance_CheckInBtn"]    = "Inchecken via RFID",
        ["Attendance_SimulateGps"]   = "Simuleer aankomst bij locatie",
        ["Attendance_CheckedIn"]     = "Aanwezig geregistreerd ✓",
        ["Attendance_NotYetOpen"]    = "Inchecken beschikbaar vanaf {0:HH:mm}",
        ["Attendance_LessonEnded"]   = "De les is beëindigd",
        ["Attendance_AlertTitle"]    = "Ingecheckt!",
        ["Attendance_AlertBody"]     = "Je aanwezigheid bij {0} is geregistreerd.",
        ["Attendance_GpsAlertTitle"] = "Locatie gedetecteerd",
        ["Attendance_GpsAlertBody"]  = "Je aanwezigheid is automatisch geregistreerd via locatie.",
        ["Notif_CheckedInTitle"]     = "Aanwezigheid geregistreerd",
        ["Notif_CheckedInBody"]      = "Je aanwezigheid bij {0} op {1:HH:mm} is geregistreerd.",
    };

    private static readonly Dictionary<string, string> En = new()
    {
        // ── Common ──
        ["Common_Cancel"]  = "Cancel",
        ["Common_Yes"]     = "Yes",
        ["Common_No"]      = "No",
        ["Common_Error"]   = "Error",
        ["Common_Success"] = "Success",

        // ── Login ──
        ["Login_Subtitle"]            = "Log in to continue",
        ["Login_EmailPlaceholder"]    = "Email address",
        ["Login_PasswordPlaceholder"] = "Password",
        ["Login_Button"]              = "Log in",
        ["Login_Footer"]              = "Want to become a member? Sign up via the website.",

        // ── Home ──
        ["Home_Title"]           = "Main menu",
        ["Home_Schedule"]        = "Schedule",
        ["Home_Reservations"]    = "Appointments & reservations",
        ["Home_Profile"]         = "Profile",
        ["Home_Subscription"]    = "Subscription",
        ["Home_TrainerSchedule"] = "Trainer Schedule",
        ["Home_MyLessons"]       = "My Lessons",
        ["Home_CreateLesson"]    = "Create Lesson",
        ["Home_ManageLessons"]   = "Lesson Management",
        ["Home_Instructors"]     = "Instructors",
        ["Home_Members"]         = "Members",
        ["Home_WelcomeAdmin"]    = "Welcome, Admin",
        ["Home_WelcomeTrainer"]  = "Welcome, Trainer",
        ["Home_WelcomeMember"]   = "Welcome to FitLife",

        // ── Flyout menu ──
        ["Menu_Home"]         = "Home",
        ["Menu_Settings"]     = "Settings",
        ["Menu_MySportclub"]  = "My sports club",
        ["Menu_Logout"]       = "Log out",
        ["Menu_MyAccount"]    = "My account",
        ["Menu_Reservations"] = "Appointments & reservations",
        ["Menu_Schedule"]     = "Schedule",
        ["Role_Admin"]        = "Administrator",
        ["Role_Instructor"]   = "Instructor",
        ["Role_Member"]       = "Member",
        ["Logout_Title"]      = "Log out",
        ["Logout_Confirm"]    = "Are you sure you want to log out?",
        ["Logout_Yes"]        = "Yes, log out",

        // ── Settings ──
        ["Settings_Title"]               = "Settings",
        ["Settings_Language"]            = "LANGUAGE",
        ["Settings_Notifications"]       = "NOTIFICATIONS",
        ["Settings_Waitlist"]            = "Waitlist",
        ["Settings_WaitlistDesc"]        = "Notification when a spot opens up",
        ["Settings_Push"]                = "Push notifications",
        ["Settings_PushDesc"]            = "All app notifications",
        ["Settings_Cancelled"]           = "Cancelled classes",
        ["Settings_CancelledDesc"]       = "Notification when a lesson is cancelled",
        ["Settings_News"]                = "General & news",
        ["Settings_NewsDesc"]            = "Updates and announcements from FitLife",
        ["Settings_Location"]            = "LOCATION",
        ["Settings_LocationSharing"]     = "Share location",
        ["Settings_LocationSharingDesc"] = "Permission for automatic check-in",
        ["Settings_LocationInfo"]        = "FitLife will ask your device for permission once you turn this on. You can always change this in your phone's app settings.",

        // ── My sports club ──
        ["Sportclub_Title"]      = "My Sports Club",
        ["Sportclub_Directions"] = "Directions",

        // ── Profile ──
        ["Profile_Title"]              = "My Profile",
        ["Profile_ChangePhoto"]        = "Change photo",
        ["Profile_DeletePhoto"]        = "Delete photo",
        ["Profile_CreditEqualsLesson"] = "1 credit = 1 lesson",
        ["Profile_Credits"]            = "credits",
        ["Profile_MySubscription"]     = "My Subscription",
        ["Profile_Type"]               = "Type:",
        ["Profile_RemainingCredits"]   = "Credits left this month:",
        ["Profile_Renewal"]            = "Renewal:",
        ["Profile_ManageSubscription"] = "Manage Subscription",
        ["Profile_ViewMyLessons"]      = "View My Lessons",
        ["Profile_NoSubscription"]     = "No subscription",

        // ── Notifications ──
        ["Notifications_Title"]     = "Notifications",
        ["Notifications_AllRead"]   = "Mark all read",
        ["Notifications_Empty"]     = "No notifications",
        ["Notifications_EmptyDesc"] = "You'll see your notifications here as soon as they arrive.",

        // ── Subscription ──
        ["Subscription_Title"]            = "Subscription",
        ["Subscription_Current"]          = "Current Subscription",
        ["Subscription_ManageCurrent"]    = "⚙️ Manage Current Subscription",
        ["Subscription_ChangeBilling"]    = "Change billing cycle",
        ["Subscription_Cancel"]           = "Cancel subscription",
        ["Subscription_ChangesNote"]      = "💡 Changes take effect on your renewal date",
        ["Subscription_ViewPrices"]       = "View Prices",
        ["Subscription_Monthly"]          = "Monthly",
        ["Subscription_Yearly"]           = "Yearly",
        ["Subscription_Save15"]           = "Save ~15%",
        ["Subscription_Choose"]           = "Choose a Subscription",
        ["Subscription_PerMonth"]         = " per month",
        ["Subscription_CreditEqualsLesson"] = "1 credit = 1 lesson",
        ["Subscription_AccessAll"]        = "Access to all lessons",
        ["Subscription_OnlineReserve"]    = "Book online via the app",
        ["Subscription_CreditsRoll"]      = "Credits roll over (max 1 month)",
        ["Subscription_FreeDrink"]        = "Free sports drink",
        ["Subscription_WaitlistPriority"] = "Priority on waitlists",
        ["Subscription_GuestMonthly"]     = "1 guest visit per month",
        ["Subscription_SelectThis"]       = "Choose this subscription",
        ["Subscription_HowTitle"]         = "ℹ️ How do subscription changes work?",
        ["Subscription_HowText"]          = "• You can switch subscriptions at any time\n• The change takes effect on your next renewal date\n• You keep your current subscription until that date\n• Credits are adjusted automatically upon renewal",
        ["Subscription_NoExpiry"]         = "No expiry date",

        // ── My lessons ──
        ["MyLessons_Title"]        = "My Lessons",
        ["MyLessons_HistoryTab"]   = "History",
        ["MyLessons_EmptyTitle"]   = "No upcoming lessons",
        ["MyLessons_EmptyDesc"]    = "Book a lesson via the schedule to see it here.",
        ["MyLessons_Cancel"]       = "Cancel",
        ["MyLessons_Total"]        = "Total",
        ["MyLessons_ThisMonth"]    = "This month",
        ["MyLessons_ThisYear"]     = "This year",
        ["MyLessons_MostVisited"]  = "Most attended lesson",
        ["MyLessons_Attended"]     = "Attended lessons",
        ["MyLessons_NoneAttended"] = "No lessons attended yet",

        // ── Week/schedule ──
        ["Week_CalendarView"] = "Calendar view",
        ["Week_ListView"]     = "List view",

        // ── Day ──
        ["Day_Title"]   = "Day Overview",
        ["Day_Refresh"] = "Refresh",

        // ── Lessons ──
        ["Lessons_NoneFound"] = "No lessons found",
        ["Lessons_Refresh"]   = "Refresh",

        // ── Lesson detail ──
        ["Detail_Register"]       = "Sign up",
        ["Detail_RegisterClosed"] = "Sign-up no longer possible",
        ["Detail_Unregister"]     = "Cancel registration",
        ["Detail_CancelDeadline"] = "Cancellation is no longer possible (less than 1 hour before start).",
        ["Detail_Trainer"]        = "Trainer",
        ["Detail_PaymentMethod"]  = "Payment method",
        ["Detail_EndTime"]        = "End time",
        ["Detail_Participants"]   = "Participants",
        ["Detail_AdditionalInfo"] = "Additional information",
        ["Detail_CancelInfo"]     = "You can cancel up to 1 hour before the lesson starts.",
        ["Detail_ReserveInfo"]    = "Booking is possible up to 1 week in advance.",

        // ── Participants ──
        ["Participants_Title"]    = "Participants",
        ["Participants_Buddies"]  = "Buddies",
        ["Participants_Waitlist"] = "Waitlist",

        // ── Trainer schedule ──
        ["Instructor_Title"]     = "Trainer Schedule",
        ["Instructor_NewLesson"] = "+ New Lesson",
        ["Instructor_Edit"]      = "Edit",
        ["Instructor_Delete"]    = "Delete",

        // ── Manage lesson ──
        ["Manage_Loading"]         = "Loading data...",
        ["Manage_WorkoutType"]     = "WORKOUT TYPE",
        ["Manage_Date"]            = "DATE",
        ["Manage_StartTime"]       = "START TIME",
        ["Manage_EndTime"]         = "END TIME",
        ["Manage_MaxParticipants"] = "MAX. PARTICIPANTS",
        ["Manage_MaxPlaceholder"]  = "e.g. 15",
        ["Manage_Location"]        = "LOCATION / ROOM",
        ["Manage_Instructor"]      = "INSTRUCTOR",
        ["Manage_AddMember"]       = "ADD MEMBER TO LESSON",
        ["Manage_UserId"]          = "User ID",
        ["Manage_Add"]             = "Add",

        // ── Legend ──
        ["Legend_Registered"] = "Registered",
        ["Legend_Waitlist"]   = "On waitlist",

        // ── Common alerts / buttons ──
        ["Common_OK"]          = "OK",
        ["Common_Info"]        = "Info",
        ["Common_Unlimited"]   = "Unlimited",
        ["Common_Unknown"]     = "Unknown",
        ["Common_NotLoggedIn"] = "Not logged in",
        ["Common_LoginAgain"]  = "Please log in again.",
        ["Common_ErrorRetry"]  = "Something went wrong. Please try again later.",
        ["Common_Loading"]     = "Loading...",

        // ── Login (validation) ──
        ["Login_EnterEmail"]    = "Enter your email address.",
        ["Login_EnterPassword"] = "Enter your password.",
        ["Login_GenericError"]  = "Something went wrong. Please try again.",

        // ── Lesson detail (alerts) ──
        ["Detail_PageTitle"]            = "Group event",
        ["Detail_CreditsLeft"]          = "{0} credits left",
        ["Detail_SubLineUnlimited"]     = "{0} · Unlimited lessons",
        ["Detail_SubLineCredit"]        = "{0} · 1 credit per lesson",
        ["Detail_Expires"]              = "Expires: {0:d MMMM yyyy}",
        ["Detail_BookableFrom"]         = "Bookable from {0:d MMMM}",
        ["Detail_NotPossibleTitle"]     = "Not possible",
        ["Detail_TooFarBody"]           = "You can book at most 1 week in advance.\n{0}.",
        ["Detail_LoginToReserve"]       = "Log in again to book a lesson.",
        ["Detail_ReservedDefault"]      = "You are registered for this lesson.",
        ["Detail_ReservedAlertTitle"]   = "Registered!",
        ["Detail_ReserveFailedTitle"]   = "Registration failed",
        ["Detail_ReserveFailedBody"]    = "You could not be registered.",
        ["Detail_CancelNotPossibleTitle"] = "Cancellation not possible",
        ["Detail_CancelDeadlineBody"]   = "You can no longer cancel. Cancellation is only possible up to 1 hour before the lesson starts.",
        ["Detail_LoginToCancel"]        = "You are no longer logged in. Log in again to cancel.",
        ["Detail_UnregisteredTitle"]    = "Cancelled",
        ["Detail_UnregisteredBody"]     = "You have successfully cancelled.",
        ["Detail_CancelFailedTitle"]    = "Cancellation failed",
        ["Detail_CancelFailedBody"]     = "You could not be cancelled.",
        ["Detail_OnWaitlistNow"]        = "You are now on the waitlist.",
        ["Detail_LessonFullWaitlistTitle"] = "Lesson is full",
        ["Detail_LessonFullWaitlistBody"]  = "The lesson is full. You have been automatically added to the waitlist.",
        ["Detail_AlreadyOnWaitlist"]       = "You are already on the waitlist for this lesson.",
        ["Detail_WaitlistFailedTitle"]     = "Waitlist failed",
        ["Detail_WaitlistFailedBody"]      = "You could not be added to the waitlist. Please try again later.",

        // ── Notification content ──
        ["Notif_ReservedTitle"]    = "Registered for lesson",
        ["Notif_ReservedBody"]     = "You are registered for {0} on {1:d MMMM 'at' HH:mm}.",
        ["Notif_WaitlistTitle"]    = "On the waitlist",
        ["Notif_WaitlistBody"]     = "You are on the waitlist for {0} on {1:d MMMM 'at' HH:mm}. We'll let you know when a spot opens up.",
        ["Notif_ExpiringTitle"]    = "Subscription expiring soon",
        ["Notif_ExpiringBody"]     = "Your {0} subscription expires on {1:d MMMM yyyy}. Renew in time to keep training.",

        // ── Notification time ──
        ["Time_JustNow"]   = "Just now",
        ["Time_MinAgo"]    = "{0} min ago",
        ["Time_HourAgo"]   = "{0} hr ago",

        // ── My lessons (alerts) ──
        ["MyLessons_ConfirmTitle"]  = "Confirm",
        ["MyLessons_ConfirmBody"]   = "Are you sure you want to cancel the booking for {0}?",
        ["MyLessons_LoginToCancel"] = "You are no longer logged in. Log in again to cancel your booking.",
        ["MyLessons_CancelledTitle"]= "Cancelled",
        ["MyLessons_CancelledBody"] = "Your booking has been cancelled.",
        ["MyLessons_CancelFailedTitle"] = "Cancellation failed",
        ["MyLessons_CancelFailedBody"]  = "The booking could not be cancelled.",

        // ── Profile (alerts) ──
        ["Profile_PhotoUpdated"]      = "Profile photo updated.",
        ["Profile_PhotoUploadFailed"] = "Photo upload failed. Check your connection.",
        ["Profile_DeletePhotoTitle"]  = "Delete photo",
        ["Profile_DeletePhotoBody"]   = "Are you sure you want to delete your profile photo?",
        ["Profile_DeleteBtn"]         = "Delete",
        ["Profile_PhotoDeleteFailed"] = "Photo deletion failed. Check your connection.",
        ["Profile_PendingChange"]     = "Changing to {0} ({1}) on {2:dd-MM-yyyy}",

        // ── Subscription (alerts + display) ──
        ["Subscription_NotLoggedInTitle"]  = "Not logged in",
        ["Subscription_ExpiresOn"]         = "Expires on: {0:dd-MM-yyyy}",
        ["Subscription_PendingMessage"]    = "Your subscription will change to {0} ({1}) on {2:dd-MM-yyyy}",
        ["Subscription_MustLoginChange"]   = "You must be logged in to change your subscription.",
        ["Subscription_AlreadyHave"]       = "You already have this subscription.",
        ["Subscription_ChangeTitle"]       = "Change subscription",
        ["Subscription_ChangeConfirm"]     = "Are you sure you want to switch to {0} ({1}, €{2:F0})?\n\nThe change takes effect on {3:dd-MM-yyyy}.",
        ["Subscription_YesChange"]         = "Yes, change",
        ["Subscription_ChangeSuccess"]     = "{0}\n\nAny remaining balance of your current subscription will be settled automatically on the next direct debit.",
        ["Subscription_SuccessTitle"]      = "Success!",
        ["Subscription_MustLoginCancel"]   = "You must be logged in to cancel your subscription.",
        ["Subscription_NoActive"]          = "You have no active subscription.",
        ["Subscription_CancelTitle"]       = "Cancel subscription",
        ["Subscription_CancelConfirm"]     = "Are you sure you want to cancel your {0} subscription?\n\nYour subscription stays active until {1:dd-MM-yyyy}. After that date it will not be renewed.",
        ["Subscription_YesCancel"]         = "Yes, cancel",
        ["Subscription_MustLoginBilling"]  = "You must be logged in to change your billing cycle.",
        ["Subscription_BillingTitle"]      = "Change billing cycle",
        ["Subscription_BillingConfirm"]    = "Are you sure you want to switch from {0} to {1} billing?\n\nYou keep your current {2} subscription.\n\nThe change takes effect on {3:dd-MM-yyyy}.",
        ["Subscription_Loading"]           = "Loading...",
        ["Billing_Yearly"]                 = "yearly",
        ["Billing_Monthly"]                = "monthly",
        ["Subscription_UnlimitedCredits"]  = "Unlimited credits",
        ["Subscription_CreditsPerMonth"]   = "{0} credits per month",
        ["Subscription_UnlimitedLessons"]  = "Unlimited lessons per month",
        ["Subscription_LessonsPerMonth"]   = "{0} lessons per month",

        // ── Trainer lessons (alerts) ──
        ["Instructor_Empty"]          = "You have no scheduled lessons as a trainer.",
        ["Instructor_DeleteTitle"]    = "Delete",
        ["Instructor_DeleteConfirm"]  = "Are you sure you want to delete '{0}' on {1:dd-MM-yyyy HH:mm}?",
        ["Instructor_YesDelete"]      = "Yes, delete",
        ["Instructor_DeletedTitle"]   = "Deleted",

        // ── Manage lesson (alerts) ──
        ["Manage_LoadFailed"]        = "⚠️ Could not load data. Check the API connection.",
        ["Manage_LoadError"]         = "⚠️ Error while loading: {0}",
        ["Manage_WorkoutsLoading"]   = "Loading workouts...",
        ["Manage_ChooseWorkout"]     = "Choose a workout",
        ["Manage_LocationsLoading"]  = "Loading locations...",
        ["Manage_ChooseLocation"]    = "Choose a location",
        ["Manage_InstructorsLoading"]= "Loading instructors...",
        ["Manage_ChooseInstructor"]  = "Choose an instructor",
        ["Manage_SelectWorkout"]     = "Select a workout type.",
        ["Manage_SelectLocation"]    = "Select a location.",
        ["Manage_SelectInstructor"]  = "Select an instructor.",
        ["Manage_EndAfterStart"]     = "End time must be after the start time.",
        ["Manage_UpdatedTitle"]      = "Updated",
        ["Manage_CreatedTitle"]      = "Lesson created",
        ["Manage_SaveFirst"]         = "Save the lesson first before adding members.",
        ["Manage_InvalidUserId"]     = "Enter a valid user ID.",
        ["Manage_CreateTitle"]       = "Create Lesson",
        ["Manage_EditTitle"]         = "Edit Lesson",
        ["Manage_SaveCreate"]        = "Create Lesson",
        ["Manage_SaveUpdate"]        = "Save Lesson",
        ["Manage_PickWorkout"]       = "Select workout...",
        ["Manage_PickLocation"]      = "Select location...",
        ["Manage_PickInstructor"]    = "Select instructor...",

        // ── Participants (display) ──
        ["Participants_Stats"]       = "{0}/{1} Participants",

        // ── Week / schedule (display) ──
        ["Week_RangeText"]           = "Week {0}, {1:MMMM yyyy}",

        // ── Spinning bike selection ──
        ["Spinning_SelectBikeTitle"] = "Choose your bike",
        ["Spinning_Row"]             = "Row",
        ["Spinning_Bike"]            = "Bike",
        ["Spinning_BikeLabel"]       = "Row {0} - Bike {1}",
        ["Spinning_BikeTaken"]       = "Taken",
        ["Spinning_YourBike"]        = "Your bike: {0}",
        ["Spinning_LegendFree"]      = "Free",
        ["Spinning_LegendYours"]     = "Your bike",
        ["Spinning_LegendTaken"]     = "Taken",

        // ── Attendance registration ──
        ["Attendance_SectionTitle"]  = "ATTENDANCE",
        ["Attendance_CheckInBtn"]    = "Check in via RFID",
        ["Attendance_SimulateGps"]   = "Simulate arrival at location",
        ["Attendance_CheckedIn"]     = "Attendance registered ✓",
        ["Attendance_NotYetOpen"]    = "Check-in available from {0:HH:mm}",
        ["Attendance_LessonEnded"]   = "The lesson has ended",
        ["Attendance_AlertTitle"]    = "Checked in!",
        ["Attendance_AlertBody"]     = "Your attendance at {0} has been registered.",
        ["Attendance_GpsAlertTitle"] = "Location detected",
        ["Attendance_GpsAlertBody"]  = "Your attendance has been automatically registered via location.",
        ["Notif_CheckedInTitle"]     = "Attendance registered",
        ["Notif_CheckedInBody"]      = "Your attendance at {0} at {1:HH:mm} has been registered.",
    };
}
