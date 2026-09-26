namespace Jobsy.Core.Rules;

/// <summary>Derived insights readiness for candidate profile/Kompas GETs.</summary>
public static class InsightsStatuses
{
    public const string Ready = "Ready";
    public const string Updating = "Updating";

    public static bool IsUpdating(string? value)
        => string.Equals(value, Updating, StringComparison.OrdinalIgnoreCase);
}
