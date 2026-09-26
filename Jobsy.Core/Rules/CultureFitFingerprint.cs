using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Fingerprints for stored candidate×vacancy culture-fit results.</summary>
public static class CultureFitFingerprint
{
    public static string For(
        IReadOnlyList<string> culturePillars,
        CompetencyScores? competencies,
        CulturePersonalityScores? culture)
    {
        var pillars = string.Join(',',
            (culturePillars ?? [])
                .Select(p => (p ?? "").Trim().ToLowerInvariant())
                .Where(p => p.Length > 0)
                .OrderBy(p => p, StringComparer.Ordinal));
        var comp = competencies is { IsComplete: true } c
            ? string.Join('|', c.Samenwerken, c.Resultaatgerichtheid, c.Stressbestendigheid, c.Innovatie, c.Extraversie)
            : "none";
        var cult = culture is { IsComplete: true } p
            ? string.Join('|',
                p.Autonomy, p.Informal, p.Collaboration, p.Flexibility, p.Innovation, p.PeopleFirst,
                p.Openness, p.Conscientiousness, p.Extraversion, p.Agreeableness, p.EmotionalStability)
            : "none";
        return ShortHash($"{pillars}|{comp}|{cult}");
    }

    public static bool ShouldRetryFallback(bool fromOpenAi, DateTime? computedAtUtc, DateTime utcNow)
        => CandidateInsightsFingerprint.ShouldRetryFallback(fromOpenAi, computedAtUtc, utcNow);

    private static string ShortHash(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
