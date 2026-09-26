using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Rules;

/// <summary>Fingerprints for derived candidate insights (matches, compass, role-fit).</summary>
public static class CandidateInsightsFingerprint
{
    public static string ForMatches(
        CompetencyScores? competencies,
        RiasecScores? career,
        CulturePersonalityScores? culture,
        SchwartzValuesScores? values,
        CandidatePreferencesDto? prefs,
        string? preferencesJson)
    {
        var whoAmI = competencies is { IsComplete: true } c
                     && career is { IsComplete: true } r
                     && culture is { IsComplete: true } cult
            ? WhoAmICompleteness.Fingerprint(c, r, cult, WhoAmIProfileHighlights.FromPreferences(prefs), values)
            : "incomplete";
        var travel = prefs?.MaxTravelMinutes?.ToString() ?? "";
        var transport = prefs?.PreferredTransport ?? "";
        var roles = string.Join(',', prefs?.Roles ?? []);
        var payload = $"{whoAmI}|{travel}|{transport}|{roles}|{StableHash(preferencesJson)}";
        return ShortHash(payload);
    }

    public static string ForCompass(RiasecScores? career, bool fromDeepAnalysis)
    {
        if (career is not { IsComplete: true })
        {
            return "empty";
        }

        var payload = string.Join('|',
            career.Realistic,
            career.Investigative,
            career.Artistic,
            career.Social,
            career.Enterprising,
            career.Conventional,
            fromDeepAnalysis ? "1" : "0");
        return ShortHash(payload);
    }

    public static string ForRoleFit(
        string jobTitle,
        Guid? vacancyId,
        CompetencyScores competencies,
        RiasecScores career,
        CulturePersonalityScores? culture,
        CandidatePreferencesDto? prefs)
    {
        var title = (jobTitle ?? "").Trim().ToLowerInvariant();
        var cult = culture is { IsComplete: true }
            ? string.Join('|', culture.Autonomy, culture.Collaboration, culture.Flexibility, culture.Innovation)
            : "";
        var prefsBit = $"{prefs?.MaxTravelMinutes}|{prefs?.PreferredTransport}|{string.Join(',', prefs?.Roles ?? [])}";
        var payload = string.Join('|',
            title,
            vacancyId,
            competencies.Samenwerken,
            competencies.Resultaatgerichtheid,
            competencies.Stressbestendigheid,
            competencies.Innovatie,
            competencies.Extraversie,
            career.Realistic,
            career.Investigative,
            career.Artistic,
            career.Social,
            career.Enterprising,
            career.Conventional,
            cult,
            prefsBit);
        return ShortHash(payload);
    }

    public static bool ShouldRetryFallback(bool fromOpenAi, DateTime? generatedAtUtc, DateTime utcNow)
        => !fromOpenAi
           && generatedAtUtc is DateTime at
           && utcNow - at >= TimeSpan.FromHours(1);

    private static string StableHash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "0";
        }

        return ShortHash(value);
    }

    private static string ShortHash(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
