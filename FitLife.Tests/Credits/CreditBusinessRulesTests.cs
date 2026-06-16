namespace FitLife.Tests.Credits;

/// <summary>
/// Tests for the credit business rules implemented in the FitLife.API cancel and reserve endpoints.
/// The rules are expressed as pure functions in CreditCalculator so they can be tested
/// without any database or HTTP dependency.
/// </summary>
public class CreditBusinessRulesTests
{
    // ── MaxCreditsFor: abonnement maxima ──────────────────────────────────────

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

    // ── Creditverbruik bij inschrijven ────────────────────────────────────────

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

    // ── Creditteruggave bij annuleren (credit_used = true) ────────────────────

    [Fact]
    public void Annuleren_MetCreditUsed_GeeftEenCreditTerug()
        => Assert.Equal(6, CreditCalculator.AfterCancel(currentCredits: 5, "Intermediate", creditUsed: true));

    [Fact]
    public void Annuleren_ZonderCreditUsed_GeeftGeenCreditTerug()
        => Assert.Equal(5, CreditCalculator.AfterCancel(currentCredits: 5, "Intermediate", creditUsed: false));

    // ── Maximum credits per abonnement: nooit overschrijden ───────────────────

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

    // ── Gebruiker mag nooit meer credits terugkrijgen dan eerder verbruikt ─────

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

    // ── Beschikbare credits berekenen (display logica) ────────────────────────

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

    // ── Gebruiker mag niet meer lessen boeken dan toegestaan ──────────────────

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
