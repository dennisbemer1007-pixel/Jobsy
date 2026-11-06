using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Passport;

namespace Jobsy.Core.Rules;

public static class ReferenceVerificationRules
{
    public const int MaxRequestsPerEntryPerMonth = 3;
    public const int MaxRequestsPerCandidatePerMonth = 10;
    public const int TokenDays = 14;
    public const int MaxCompetencesOffered = 5;
    public const string ConsentVersion = "2026-10-18";

    public const string CandidateConsentText =
        "Ik vraag deze persoon om mijn werkervaring en vaardigheden te bevestigen. Lobsy stuurt een mail met een persoonlijke link. Contactgegevens van mijn referent blijven privé.";

    public const string RefereePrivacyText =
        "Lobsy vraagt dit omdat deze persoon jou noemde als referent. Je antwoorden gaan naar die persoon. We bewaren je contactgegevens niet langer dan nodig. Je hoeft niet te antwoorden.";

    public const int NameMax = 80;
    public const int RelationMax = 40;
    public const int EmailMax = 254;
    public const int CommentMax = 400;
    public const int DutiesMax = 600;

    public static bool HasWorkExperienceEntry(CandidatePreferencesDto? preferences)
    {
        if (preferences?.NoWorkExperience == true)
        {
            return false;
        }

        var jobs = preferences?.Employers ?? [];
        return jobs.Any(e =>
            !string.IsNullOrWhiteSpace(e.EmployerName)
            && (!string.IsNullOrWhiteSpace(e.Role) || !string.IsNullOrWhiteSpace(e.StartMonth)));
    }

    public static bool FeatureEligible(CandidatePreferencesDto? preferences, bool allTestsComplete)
    {
        if (!allTestsComplete)
        {
            return false;
        }

        return HasWorkExperienceEntry(preferences);
    }

    public static string? NormalizeName(string? value) => Clean(value, NameMax, 2);
    public static readonly string[] RelationCodes = ["leidinggevende", "collega", "opdrachtgever", "anders"];

    public static string? NormalizeRelation(string? value)
    {
        var cleaned = Clean(value, RelationMax, 2);
        if (cleaned is null)
        {
            return null;
        }

        var code = cleaned.Trim().ToLowerInvariant();
        return RelationCodes.Contains(code, StringComparer.Ordinal) ? code : null;
    }
    public static string? NormalizeComment(string? value) => Clean(value, CommentMax, 0);
    public static string? NormalizeDuties(string? value) => Clean(value, DutiesMax, 0);

    public static bool IsValidEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && email.Length <= EmailMax
           && email.Contains('@', StringComparison.Ordinal)
           && !email.Contains(' ', StringComparison.Ordinal);

    public static string DisplayStatus(WorkExperienceReferenceRequest row)
    {
        if (row.RemovedByCandidate || row.Status == WorkExperienceReferenceStatus.Revoked)
        {
            return "revoked";
        }

        if (row.Status == WorkExperienceReferenceStatus.Confirmed)
        {
            return "confirmed";
        }

        if (row.Status == WorkExperienceReferenceStatus.Declined)
        {
            return "declined";
        }

        if (row.Status == WorkExperienceReferenceStatus.Pending && row.CompletedAtUtc is null)
        {
            return "sent";
        }

        return row.Status;
    }

    public static bool IsActiveVerification(WorkExperienceReferenceRequest row)
        => !row.RemovedByCandidate
           && row.Status == WorkExperienceReferenceStatus.Confirmed
           && row.CompletedAtUtc is not null
           && row.ConfirmedEmployer == true
           && row.ConfirmedRole == true
           && row.ConfirmedPeriod == true;

    public static IReadOnlyList<string> ParseEndorsedCodes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeEndorsedCodes(IEnumerable<string> codes)
        => JsonSerializer.Serialize(codes.Distinct(StringComparer.Ordinal).Take(MaxCompetencesOffered).ToArray());

    public static string EndorsementLine(string language, string referentName, string relation, string employer)
    {
        var name = referentName.Trim();
        var rel = relation.Trim();
        var work = employer.Trim();
        return language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? $"Confirmed by {name}, {rel} at {work}"
            : $"Bevestigd door {name}, {rel} bij {work}";
    }

    private static string? Clean(string? value, int max, int min)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return min == 0 ? null : null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length < min)
        {
            return null;
        }

        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public static class WorkExperienceEntryIds
{
    public static IReadOnlyList<CandidateEmployerHistoryDto> EnsureIds(IReadOnlyList<CandidateEmployerHistoryDto>? employers)
    {
        if (employers is null || employers.Count == 0)
        {
            return [];
        }

        return employers
            .Select(e => e with { EntryId = e.EntryId ?? Guid.NewGuid() })
            .ToList();
    }

    public static Guid ResolveEntryId(CandidateEmployerHistoryDto employer)
        => employer.EntryId ?? Guid.Empty;
}
