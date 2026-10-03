using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Normalizes the shareable preference members. Null incoming members keep the stored value.</summary>
public static class ShareablePreferenceNormalizer
{
    public static SharedWorkPreferences? NormalizeWork(SharedWorkPreferences? incoming, SharedWorkPreferences? existing)
    {
        if (incoming is null)
        {
            return existing;
        }

        var indoor = WorkPreferenceCatalogs.CanonicalIndoor(incoming.Indoor);
        var outdoor = WorkPreferenceCatalogs.CanonicalOutdoor(incoming.Outdoor);
        var physical = WorkPreferenceCatalogs.CanonicalPhysicalWork(incoming.PhysicalWork);
        var pace = WorkPreferenceCatalogs.CanonicalPace(incoming.Pace);
        if (indoor is null && outdoor is null && physical is null && pace is null)
        {
            return null;
        }

        return new SharedWorkPreferences(indoor, outdoor, physical, pace);
    }

    public static string? NormalizeRegion(string? incoming, string? existing)
        => incoming is null ? existing : WorkRegionRules.Sanitize(incoming);

    public static IReadOnlyList<string>? NormalizeContracts(
        IReadOnlyList<string>? incoming,
        IReadOnlyList<string>? existing)
        => incoming is null ? existing : WorkPreferenceCatalogs.NormalizeContracts(incoming);
}
