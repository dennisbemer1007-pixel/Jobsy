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
}
