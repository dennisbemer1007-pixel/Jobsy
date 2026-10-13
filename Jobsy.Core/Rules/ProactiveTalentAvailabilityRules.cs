using System.Globalization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;

namespace Jobsy.Core.Rules;

/// <summary>
/// HARD RULE: only candidates with OpenForWork (beschikbaar) may appear in proactive employer shortlists.
/// </summary>
public static class ProactiveTalentAvailabilityRules
{
    public static bool IsEligibleForEmployerShortlist(User user)
        => user.Role == UserRole.Candidate
           && user.IsActive
           && user.OpenForWork
           && user.TalentPoolConsentAt is not null
           && CandidateConsentRules.CanAppearInTalentPool(user);

    public static IEnumerable<User> FilterEligible(IEnumerable<User> candidates)
        => candidates.Where(IsEligibleForEmployerShortlist);

    /// <summary>Employer shortlist line: start (Per direct / Vanaf dd-MM-yyyy) plus optional hours/day-parts.</summary>
    public static string FormatShortlistAvailabilityLine(
        User user,
        IReadOnlyDictionary<string, string[]>? availability,
        bool flexibleTimes,
        decimal? minHours,
        decimal? maxHours,
        IReadOnlyList<string>? presets)
    {
        var start = AvailabilityStartRules.IsImmediate(user.AvailableFromDate)
            ? "Per direct"
            : $"Vanaf {user.AvailableFromDate!.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}";

        var extra = LobsyCvModelFactory.FormatTalentPoolAvailability(
            availability,
            flexibleTimes,
            minHours,
            maxHours,
            presets);
        if (string.IsNullOrWhiteSpace(extra))
        {
            return start;
        }

        if (extra.Equals("Per direct", StringComparison.OrdinalIgnoreCase)
            || extra.StartsWith("Per direct ·", StringComparison.OrdinalIgnoreCase))
        {
            return extra;
        }

        return $"{start} · {extra}";
    }
}
