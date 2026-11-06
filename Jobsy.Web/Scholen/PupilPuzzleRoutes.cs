namespace Jobsy.Web.Scholen;

/// <summary>
/// Groep 7/8 puzzle pages (schelpenrij, schatkaart, vuurtorenlampen).
/// Reis navigates to <c>/leerling/puzzel/{key}</c> only when <see cref="IsBuilt"/> is true.
/// </summary>
public static class PupilPuzzleRoutes
{
    public static bool IsBuilt(string? key)
        => !string.IsNullOrWhiteSpace(key) && Known.Contains(key);

    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "schelpenrij",
        "schatkaart",
        "vuurtorenlampen",
    };
}
