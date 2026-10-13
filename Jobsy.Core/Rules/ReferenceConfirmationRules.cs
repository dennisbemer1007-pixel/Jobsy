using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Referee confirmation limits and the partner-passport projection.
/// A partner only sees answers the candidate chose to share. No scores and no generated text.
/// </summary>
public static class ReferenceConfirmationRules
{
    public const int MaxRequestsPerReference = 3;
    public const int MaxRequestsPerCandidatePerMonth = 10;
    public const int TokenDays = 14;
    public const string ConsentVersion = "2026-10-04";

    public const string CandidateConsentText =
        "Ik vraag deze persoon om te bevestigen dat ik daar werkte. Lobsy stuurt een mail met 5 korte vragen. Ik kies later zelf of een partner de antwoorden ziet.";

    public const string RefereePrivacyText =
        "Lobsy vraagt dit omdat deze persoon jou noemde. Je antwoorden gaan naar die persoon. Die persoon kiest of een partner ze ziet. We bewaren dit zolang het account bestaat. Je hoeft niet te antwoorden.";

    public const int RoleMax = 80;
    public const int PeriodMax = 120;
    public const int DidWellMax = 400;
    public const int ExtraMax = 400;
    public const int MisuseMax = 500;

    public static string? NormalizeRole(string? value) => Clean(value, RoleMax, 2);
    public static string? NormalizePeriod(string? value) => Clean(value, PeriodMax, 2);
    public static string? NormalizeDidWell(string? value) => Clean(value, DidWellMax, 2);
    public static string? NormalizeExtra(string? value) => Clean(value, ExtraMax, 0);
    public static string? NormalizeMisuse(string? value) => Clean(value, MisuseMax, 0);

    public static string? NormalizeWorkAgain(string? value)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        return raw switch
        {
            "yes" or "ja" => "yes",
            "no" or "nee" => "no",
            "maybe" or "misschien" => "maybe",
            _ => null
        };
    }

    /// <summary>
    /// Answers a partner may see. Null when the candidate has not turned sharing on,
    /// or the referee has not confirmed. Unshared answers stay null. Email and phone never leave.
    /// </summary>
    public static PartnerReferenceFact? ForPartner(
        ReferenceConfirmation? confirmation,
        string employerName,
        string refereeName)
    {
        if (confirmation is null
            || confirmation.Status != ReferenceConfirmationStatus.Confirmed
            || confirmation.ConfirmedAtUtc is null
            || !confirmation.ShowOnPartnerPassport)
        {
            return null;
        }

        return new PartnerReferenceFact(
            employerName,
            refereeName,
            confirmation.RoleTitle,
            confirmation.ConfirmedAtUtc.Value,
            confirmation.ShareWorkedHere ? confirmation.WorkedHere : null,
            confirmation.SharePeriod ? confirmation.PeriodText : null,
            confirmation.ShareDidWell ? confirmation.DidWell : null,
            confirmation.ShareWorkAgain ? confirmation.WorkAgain : null,
            confirmation.ShareExtra ? confirmation.ExtraText : null);
    }

    private static string? Clean(string? value, int max, int min)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return min == 0 ? null : null;
        }

        var trimmed = value.Trim().Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        while (trimmed.Contains("  ", StringComparison.Ordinal))
        {
            trimmed = trimmed.Replace("  ", " ", StringComparison.Ordinal);
        }

        if (trimmed.Length > max)
        {
            trimmed = trimmed[..max].Trim();
        }

        if (trimmed.Length < min)
        {
            return null;
        }

        return trimmed;
    }
}

/// <summary>Facts a partner passport may show. Only what the candidate confirmed and chose to share.</summary>
public sealed record PartnerReferenceFact(
    string EmployerName,
    string RefereeName,
    string RoleTitle,
    DateTime ConfirmedAtUtc,
    bool? WorkedHere,
    string? Period,
    string? DidWell,
    string? WorkAgain,
    string? Extra);
