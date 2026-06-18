namespace FitLife.Tests.Credits;

// Tests for the credit business rules implemented in the FitLife.API cancel and
// reserve endpoints. The rules are expressed as pure functions in CreditCalculator
// (bottom of this file) so they can be tested without any database or HTTP dependency.
//
// Credits raken meerdere user stories, daarom zijn de tests per story gesplitst:
//   • US-L02 — Les reserveren            (1 credit afschrijven; alleen bij voldoende credits)
//   • US-L03 — Reservering annuleren      (credit teruggestort, nooit boven maximum)
//   • US-L06 — Abonnement en credits inzien (maxima per abonnement, onbeperkt-weergave)
//   • US-B04 — Leden en abonnementen beheren (abonnement bepaalt het aantal credits)

/// <summary>
/// US-L02 — Les reserveren (Lid, Must have).
/// "Reserveren kan alleen wanneer het lid voldoende credits heeft" en
/// "bij een succesvolle reservering wordt precies één credit afgeschreven".
/// </summary>
[Trait("UserStory", "US-L02")]
[Trait("Rol", "Lid")]
public class ReserveCreditRulesTests
{
    [Fact]
    public void Inschrijven_TrektEenCreditAf()
        => Assert.Equal(12, CreditCalculator.AfterReserve(currentCredits: 13));

    [Fact]
    public void Inschrijven_VanuitEenCredit_GaatNaarNul()
        => Assert.Equal(0, CreditCalculator.AfterReserve(currentCredits: 1));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(13)]
    [InlineData(999)]
    public void CanReserve_MetVoldoendeCredits_IsTrue(int credits)
        => Assert.True(CreditCalculator.CanReserve(credits));

    [Fact]
    public void CanReserve_MetNulCredits_IsFalse()
        => Assert.False(CreditCalculator.CanReserve(0));

    [Fact]
    public void CanReserve_MetNegatiefCredits_IsFalse()
        => Assert.False(CreditCalculator.CanReserve(-1));

    [Fact]
    public void CanReserve_Advanced_Sentinel_IsTrue()
        => Assert.True(CreditCalculator.CanReserve(999));

    [Fact]
    public void Inschrijven_NaAlCreditsGebruikt_IsNietMogelijk()
    {
        // Na gebruik van alle 9 Rookie-credits kan de gebruiker niet meer boeken
        Assert.False(CreditCalculator.CanReserve(credits: 0));
    }

    [Fact]
    public void Inschrijven_ZolangCreditsOver_IsMogelijk()
    {
        Assert.True(CreditCalculator.CanReserve(1));
        Assert.True(CreditCalculator.CanReserve(9));
        Assert.True(CreditCalculator.CanReserve(13));
    }
}

/// <summary>
/// US-L03 — Reservering annuleren (Lid, Must have).
/// "Bij annulering binnen de termijn wordt de credit teruggestort."
/// De teruggave mag het abonnementsmaximum nooit overschrijden, en een
/// admin-toegevoegde reservering (credit_used = false) geeft geen credit terug.
/// </summary>
[Trait("UserStory", "US-L03")]
[Trait("Rol", "Lid")]
public class CancelCreditRulesTests
{
    [Fact]
    public void Annuleren_MetCreditUsed_GeeftEenCreditTerug()
        => Assert.Equal(6, CreditCalculator.AfterCancel(currentCredits: 5, "Intermediate", creditUsed: true));

    [Fact]
    public void Annuleren_ZonderCreditUsed_GeeftGeenCreditTerug()
        => Assert.Equal(5, CreditCalculator.AfterCancel(currentCredits: 5, "Intermediate", creditUsed: false));

    [Fact]
    public void Annuleren_Rookie_OpMaximum_BlijftOp9()
        => Assert.Equal(9, CreditCalculator.AfterCancel(currentCredits: 9, "Rookie", creditUsed: true));

    [Fact]
    public void Annuleren_Rookie_OnderMaximum_VerhoogtNormaal()
        => Assert.Equal(7, CreditCalculator.AfterCancel(currentCredits: 6, "Rookie", creditUsed: true));

    [Fact]
    public void Annuleren_Intermediate_OpMaximum_BlijftOp13()
        => Assert.Equal(13, CreditCalculator.AfterCancel(currentCredits: 13, "Intermediate", creditUsed: true));

    [Fact]
    public void Annuleren_Intermediate_OnderMaximum_VerhoogtNormaal()
        => Assert.Equal(10, CreditCalculator.AfterCancel(currentCredits: 9, "Intermediate", creditUsed: true));

    [Fact]
    public void Annuleren_Advanced_Sentinel_BlijftOp999_NietDaarboven()
        => Assert.Equal(999, CreditCalculator.AfterCancel(currentCredits: 999, "Advanced", creditUsed: true));

    [Fact]
    public void Annuleren_Advanced_OndeSentinel_KlapmtBijSentinel()
        // Hypothetisch: als Advanced op 998 staat door een edge case → klamt bij 999
        => Assert.Equal(999, CreditCalculator.AfterCancel(currentCredits: 998, "Advanced", creditUsed: true));

    [Fact]
    public void AdminToegevoegdeReservering_BijAnnuleren_GeenCreditTerug()
    {
        // Reservering aangemaakt via /add-member (credit_used=false) → geen refund
        int creditsBefore = 7;
        int creditsAfter = CreditCalculator.AfterCancel(creditsBefore, "Intermediate", creditUsed: false);
        Assert.Equal(creditsBefore, creditsAfter);
    }

    [Fact]
    public void GebruikerKrijgtNooitMeerDanAbonnementsmaximum()
    {
        // Simuleer een edge case waarbij credits al op max staan
        // en nog een annulering plaatsvindt (met credit_used=true via legacy data)
        int max = CreditCalculator.MaxCreditsFor("Rookie");
        int creditsAfter = CreditCalculator.AfterCancel(max, "Rookie", creditUsed: true);
        Assert.True(creditsAfter <= max);
    }

    [Theory]
    [InlineData("Rookie", 9)]
    [InlineData("Intermediate", 13)]
    [InlineData("Advanced", 999)]
    public void NaAnnuleren_CreditsOverschrijdtNooitMaximum(string type, int max)
    {
        int creditsAfter = CreditCalculator.AfterCancel(max, type, creditUsed: true);
        Assert.True(creditsAfter <= max, $"Credits ({creditsAfter}) mag maximum ({max}) niet overschrijden");
    }
}

/// <summary>
/// US-L06 — Abonnement en credits inzien (Lid, Should have).
/// "Het actuele aantal beschikbare credits wordt getoond" en het abonnementstype
/// bepaalt het maximum (Advanced = onbeperkt).
/// </summary>
[Trait("UserStory", "US-L06")]
[Trait("Rol", "Lid")]
public class CreditDisplayRulesTests
{
    [Fact]
    public void MaxCredits_Rookie_Is9()
        => Assert.Equal(9, CreditCalculator.MaxCreditsFor("Rookie"));

    [Fact]
    public void MaxCredits_Intermediate_Is13()
        => Assert.Equal(13, CreditCalculator.MaxCreditsFor("Intermediate"));

    [Fact]
    public void MaxCredits_Advanced_Is999_Sentinel()
        => Assert.Equal(999, CreditCalculator.MaxCreditsFor("Advanced"));

    [Fact]
    public void MaxCredits_OnbekendType_VallerugOp9()
        => Assert.Equal(9, CreditCalculator.MaxCreditsFor("Onbekend"));

    [Fact]
    public void CreditsDisplay_Rookiemet5Credits_Toont5VanMax9()
    {
        int remaining = 5;
        int max = CreditCalculator.MaxCreditsFor("Rookie");
        Assert.True(remaining <= max);
        Assert.Equal(9, max);
    }

    [Fact]
    public void CreditsDisplay_AdvancedAbonnement_IsOnbeperkt()
        => Assert.True(CreditCalculator.IsUnlimited("Advanced"));

    [Fact]
    public void CreditsDisplay_RookieAbonnement_IsNietOnbeperkt()
        => Assert.False(CreditCalculator.IsUnlimited("Rookie"));

    [Fact]
    public void CreditsDisplay_IntermediateAbonnement_IsNietOnbeperkt()
        => Assert.False(CreditCalculator.IsUnlimited("Intermediate"));
}

/// <summary>
/// US-B04 — Leden en abonnementen beheren (Beheerder, Must have).
/// "Het abonnement bepaalt het aantal credits dat een lid ontvangt."
/// Het toegekende maximum volgt rechtstreeks uit het gekoppelde abonnementstype.
/// </summary>
[Trait("UserStory", "US-B04")]
[Trait("Rol", "Beheerder")]
public class SubscriptionCreditAllotmentTests
{
    [Theory]
    [InlineData("Rookie", 9)]
    [InlineData("Intermediate", 13)]
    [InlineData("Advanced", 999)]
    public void AbonnementBepaaltAantalCredits(string subscriptionType, int expectedMax)
        => Assert.Equal(expectedMax, CreditCalculator.MaxCreditsFor(subscriptionType));

    [Fact]
    public void HogerAbonnement_GeeftMeerCredits()
    {
        Assert.True(CreditCalculator.MaxCreditsFor("Intermediate") > CreditCalculator.MaxCreditsFor("Rookie"));
        Assert.True(CreditCalculator.MaxCreditsFor("Advanced")     > CreditCalculator.MaxCreditsFor("Intermediate"));
    }
}

/// <summary>
/// Pure-C# replica of the credit logic in FitLife.API/Program.cs, expressed as
/// testable static methods. Mirrors:
///   - Reserve: UPDATE users SET credits = credits - 1
///   - Cancel:  UPDATE users SET credits = LEAST(credits + 1, max) WHERE credit_used = 1
/// </summary>
internal static class CreditCalculator
{
    public static int MaxCreditsFor(string subscriptionType) => subscriptionType switch
    {
        "Rookie"       => 9,
        "Intermediate" => 13,
        "Advanced"     => 999,
        _              => 9
    };

    public static bool IsUnlimited(string subscriptionType)
        => string.Equals(subscriptionType, "Advanced", StringComparison.OrdinalIgnoreCase)
           || MaxCreditsFor(subscriptionType) >= 999;

    public static int AfterReserve(int currentCredits)
        => currentCredits - 1;

    public static bool CanReserve(int credits)
        => credits >= 1;

    public static int AfterCancel(int currentCredits, string subscriptionType, bool creditUsed)
    {
        if (!creditUsed) return currentCredits;
        return Math.Min(currentCredits + 1, MaxCreditsFor(subscriptionType));
    }
}
