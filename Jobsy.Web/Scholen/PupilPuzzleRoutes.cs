namespace Jobsy.Web.Scholen;

/// <summary>
/// Groep 7/8 puzzle pages (schelpenrij, schatkaart, vuurtorenlampen) are not built.
/// The reis must not navigate to <c>/leerling/puzzel/{key}</c> until one is.
/// </summary>
public static class PupilPuzzleRoutes
{
    public static bool IsBuilt(string? key)
        => !string.IsNullOrWhiteSpace(key) && Known.Contains(key);

    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase);
}
