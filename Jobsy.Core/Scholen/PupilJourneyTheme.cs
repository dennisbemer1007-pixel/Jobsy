namespace Jobsy.Core.Scholen;

/// <summary>
/// Visual journey theme for groep 7/8 (same questions, different world).
/// Keys match approved mockups (<c>zee</c>, <c>tuin</c>, <c>ster</c>).
/// </summary>
public static class PupilJourneyTheme
{
    public const string ZeeExpeditie = "zee";
    public const string Eilandtuin = "tuin";
    public const string Sterrenkust = "ster";

    public static readonly IReadOnlyList<string> All = [ZeeExpeditie, Eilandtuin, Sterrenkust];

    public static bool IsValid(string? key)
        => !string.IsNullOrWhiteSpace(key)
           && All.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string key)
    {
        var match = All.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new ArgumentOutOfRangeException(nameof(key), "Unknown journey theme.");
    }
}
