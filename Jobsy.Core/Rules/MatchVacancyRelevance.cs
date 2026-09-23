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

        if (required.Length == 0 || string.IsNullOrWhiteSpace(candidatePreferredTransport))
        {
            return true;
        }

        var preferred = candidatePreferredTransport.Trim();
        // "Auto" preference also covers unspecified car-like labels on vacancies.
        return required.Any(r =>
            string.Equals(r, preferred, StringComparison.OrdinalIgnoreCase)
            || (string.Equals(preferred, "Auto", StringComparison.OrdinalIgnoreCase)
                && r.Contains("auto", StringComparison.OrdinalIgnoreCase)));
    }
}
