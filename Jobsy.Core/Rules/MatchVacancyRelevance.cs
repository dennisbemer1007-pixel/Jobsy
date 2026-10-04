namespace Jobsy.Core.Rules;

/// <summary>
/// Relevance filter for Match swipe decks: education hard-gate plus optional travel/transport.
/// Banenkaart stays unfiltered; Match only shows fitting vacancies.
/// </summary>
public static class MatchVacancyRelevance
{
    public static bool IsRelevant(
        IEnumerable<string>? candidateEducations,
        int? candidateMaxTravelMinutes,
        string? candidatePreferredTransport,
        string? vacancyRequiredEducation,
        int? vacancyTravelMinutes,
        IEnumerable<string>? vacancyRequiredTransport)
    {
        if (!EducationLevelLabels.CandidateMeetsRequirement(candidateEducations, vacancyRequiredEducation))
        {
            return false;
        }

        if (candidateMaxTravelMinutes is int maxMinutes
            && maxMinutes > 0
            && vacancyTravelMinutes is int travel
            && travel > maxMinutes)
        {
            return false;
        }

        var required = (vacancyRequiredTransport ?? [])
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToArray();

        var preferredModes = TransportLabels.SplitMany(candidatePreferredTransport);
        if (required.Length == 0 || preferredModes.Count == 0)
        {
            return true;
        }

        return required.Any(r => preferredModes.Any(preferred => TransportMatches(preferred, r)));
    }

    private static bool TransportMatches(string preferred, string required)
    {
        if (string.Equals(required, preferred, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var canonical = TransportLabels.TryCanonical(required);
        if (canonical is not null && string.Equals(canonical, preferred, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // "Auto" preference also covers unspecified car-like labels on vacancies.
        return string.Equals(preferred, TransportLabels.Car, StringComparison.OrdinalIgnoreCase)
               && required.Contains("auto", StringComparison.OrdinalIgnoreCase);
    }
}
