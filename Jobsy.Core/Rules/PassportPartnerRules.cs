using Jobsy.Core.Entities;
using Jobsy.Core.Privacy;

namespace Jobsy.Core.Rules;

public static class PassportPartnerRules
{
    public const int MaxActivePartners = 3;
    public const int MinimumAge = 18;
    public const int CountDisclosureThreshold = 5;

    public static readonly TimeSpan ReconfirmInterval = TimeSpan.FromDays(30 * 6);
    public static readonly TimeSpan ReconfirmReminderLead = TimeSpan.FromDays(14);
    public static readonly TimeSpan AccessLogRetention = TimeSpan.FromDays(365);

    public static bool IsActiveLink(PassportPartnerCandidateLink link)
        => link.ConsentGivenAtUtc is not null && link.RevokedAtUtc is null;

    public static DateTime NextReconfirmDue(DateTime consentAtUtc)
        => DateTime.SpecifyKind(consentAtUtc, DateTimeKind.Utc).Add(ReconfirmInterval);

    /// <summary>Exact count only at the threshold or above. 0–4 all read as the same phrase.</summary>
    public static string FormatDisclosedCount(int count)
        => count >= CountDisclosureThreshold ? count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "minder dan 5";

    public static bool IsOldEnough(int? ageYears, bool confirmAdult)
    {
        if (ageYears is int age)
        {
            return age >= MinimumAge;
        }

        return confirmAdult;
    }

    public static bool IsCurrentConsent(string? version)
        => string.Equals(version, PrivacyConstants.PartnerShareConsentVersion, StringComparison.Ordinal);
}

/// <summary>Partner terms the organisation accepts. Version bump requires a new acceptance.</summary>
public static class PassportPartnerTerms
{
    public const string CurrentVersion = "2026-10-03";

    public const string Text =
        """
        Je ziet alleen kandidaten die met jouw code zijn gestart en toestemming hebben gegeven. Je kunt niet zoeken in de Lobsy-pool.
        Je ziet het paspoort, nooit testantwoorden of ruwe scores.
        Lobsy geeft geen score, rangschikking of automatische selectie. Het paspoort is gespreksondersteuning; een mens beslist.
        Jij bent zelfstandig verwerkingsverantwoordelijke voor pdf's die je downloadt.
        Je scoort of rangschikt kandidaten niet, ook niet automatisch. Je stopt paspoortgegevens niet in eigen AI- of matchingtools. Geen white-label gebruik.
        Het paspoort bevat geen AI-uitvoer. Lobsy toont alleen feiten die de kandidaat zelf heeft ingevuld of bevestigd.
        Inloggen als partner vereist 2FA. Inloggen met Google of Microsoft telt als 2FA.
        """;
}
