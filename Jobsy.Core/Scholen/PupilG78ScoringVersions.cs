namespace Jobsy.Core.Scholen;

/// <summary>Stored on <see cref="Entities.Scholen.PupilProgress"/> and <see cref="Entities.Scholen.PupilResult"/>.</summary>
public static class PupilG78ScoringVersions
{
    public const string V1 = "g78-1";
    public const string V2 = "g78-2";

    public static bool IsKnown(string? version)
        => string.Equals(version, V1, StringComparison.Ordinal)
           || string.Equals(version, V2, StringComparison.Ordinal);

    public static string NormalizeOrDefault(string? version, bool contrastEnabledForNewSessions)
    {
        if (string.Equals(version, V2, StringComparison.Ordinal))
        {
            return V2;
        }

        if (string.Equals(version, V1, StringComparison.Ordinal))
        {
            return V1;
        }

        return contrastEnabledForNewSessions ? V2 : V1;
    }
}
