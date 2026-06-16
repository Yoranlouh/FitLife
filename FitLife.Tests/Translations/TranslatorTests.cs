using FitLife.Tests.Fakes;

namespace FitLife.Tests.Translations;

/// <summary>
/// Tests for the translation lookup logic.
/// Uses FakeTranslator which mirrors the production Translator's key-lookup and
/// fallback behaviour without any MAUI Preferences dependency.
/// </summary>
public class TranslatorTests
{
    // ── Standaardtaal: Nederlands ─────────────────────────────────────────────

    [Fact]
    public void Standaard_TaalIsNederlands()
    {
        var tr = new FakeTranslator();
        Assert.Equal("nl", tr.Language);
    }

    // ── Nederlandse teksten ───────────────────────────────────────────────────

    [Fact]
    public void NederlandsSleutel_GeeftNederlandseTekst()
    {
        var tr = new FakeTranslator();
        Assert.Equal("Inloggen", tr.Get("Login_Button"));
    }

    [Fact]
    public void NederlandsSleutel_AnnulerenKnop_GeeftNederlandseTekst()
    {
        var tr = new FakeTranslator();
        Assert.Equal("Annuleren", tr.Get("Common_Cancel"));
    }

    [Fact]
    public void NederlandsSleutel_Aanwezigheid_GeeftJuisteTekst()
    {
        var tr = new FakeTranslator();
        Assert.Equal("Inchecken via RFID", tr.Get("Attendance_CheckInBtn"));
    }

    [Fact]
    public void NederlandsSleutel_WachtlijstTekst_GeeftJuisteTekst()
    {
        var tr = new FakeTranslator();
        Assert.Equal("Les is vol", tr.Get("Detail_LessonFullWaitlistTitle"));
    }

    // ── Engelse teksten ───────────────────────────────────────────────────────

    [Fact]
    public void EngelsSleutel_NaWisselenNaarEn_GeeftEngelsTekst()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("en");
        Assert.Equal("Log in", tr.Get("Login_Button"));
    }

    [Fact]
    public void EngelsSleutel_Aanwezigheid_GeeftEngelsTekst()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("en");
        Assert.Equal("Check in via RFID", tr.Get("Attendance_CheckInBtn"));
    }

    [Fact]
    public void EngelsSleutel_WachtlijstTekst_GeeftEngelsTekst()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("en");
        Assert.Equal("Lesson full", tr.Get("Detail_LessonFullWaitlistTitle"));
    }

    [Fact]
    public void EngelsSleutel_AnnulerenKnop_GeeftEngelsTekst()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("en");
        Assert.Equal("Cancel", tr.Get("Common_Cancel"));
    }

    // ── Terugval gedrag bij ontbrekende vertalingen ───────────────────────────

    [Fact]
    public void OnbekendeSleutel_InNl_GeeftSleutelZelf()
    {
        var tr = new FakeTranslator();
        Assert.Equal("Onbekende_Sleutel", tr.Get("Onbekende_Sleutel"));
    }

    [Fact]
    public void OnbekendeSleutel_InEn_GeeftSleutelZelf()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("en");
        Assert.Equal("Onbekende_Sleutel", tr.Get("Onbekende_Sleutel"));
    }

    [Fact]
    public void LegeSleutel_GeeftLegeString()
    {
        var tr = new FakeTranslator();
        // Lege sleutel bestaat niet → terugval naar de sleutel zelf (lege string)
        Assert.Equal(string.Empty, tr.Get(string.Empty));
    }

    // ── Taalwisseling op runtime ──────────────────────────────────────────────

    [Fact]
    public void WisselenVanNlNaarEn_WijzigtGeteldeTekst()
    {
        var tr = new FakeTranslator();
        var nlText = tr.Get("Login_Button");
        tr.SetLanguage("en");
        var enText = tr.Get("Login_Button");

        Assert.NotEqual(nlText, enText);
        Assert.Equal("Inloggen", nlText);
        Assert.Equal("Log in", enText);
    }

    [Fact]
    public void WisselenVanEnNaarNl_WijzigtGeteldeTekst()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("en");
        var enText = tr.Get("Common_Unlimited");

        tr.SetLanguage("nl");
        var nlText = tr.Get("Common_Unlimited");

        Assert.Equal("Unlimited", enText);
        Assert.Equal("Onbeperkt", nlText);
    }

    [Fact]
    public void SetLanguage_MetOngeldiedeWaarde_ValterugOpNl()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("fr");  // Frans wordt niet ondersteund → terugval naar nl
        Assert.Equal("nl", tr.Language);
        Assert.Equal("Inloggen", tr.Get("Login_Button"));
    }

    // ── Opmaak-overload (format args) ─────────────────────────────────────────

    [Fact]
    public void FormatArg_CreditsDisplay_VervangArgument()
    {
        var tr = new FakeTranslator();
        var result = tr.Get("Detail_CreditsLeft", 7);
        Assert.Equal("7 credits over", result);
    }

    [Fact]
    public void FormatArg_EngelsCreditsDisplay_VervangArgument()
    {
        var tr = new FakeTranslator();
        tr.SetLanguage("en");
        var result = tr.Get("Detail_CreditsLeft", 5);
        Assert.Equal("5 credits left", result);
    }

    [Fact]
    public void FormatArg_TijdWeergave_VervangMinutenArgument()
    {
        var tr = new FakeTranslator();
        var result = tr.Get("Time_MinAgo", 15);
        Assert.Equal("15 min geleden", result);
    }

    // ── Terugval op NL als EN-sleutel ontbreekt ────────────────────────────────

    [Fact]
    public void EnSleutelOntbreekt_ValterugOpNl()
    {
        // Een sleutel die alleen in NL bestaat, niet in EN
        // FakeTranslator heeft "Notif_ReservedBody" alleen in NL
        var tr = new FakeTranslator();
        tr.SetLanguage("en");

        // NotifReservedBody exists in NL dict but NOT in EN dict of FakeTranslator
        // → fallback to NL value
        var result = tr.Get("Notif_ReservedBody");

        // Actual NL text (not the key itself) → NL fallback works
        Assert.NotEqual("Notif_ReservedBody", result);
    }
}
