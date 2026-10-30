namespace Jobsy.Web.Scholen;

/// <summary>
/// Groep 7/8 puzzle intro screens (mockup 03/06/09). Only built intros may intercept
/// navigation before <c>/leerling/puzzel/{key}</c>.
/// </summary>
public static class PupilPuzzleIntroRoutes
{
    public static bool IsBuilt(string? key)
        => string.Equals(key, "schelpenrij", StringComparison.OrdinalIgnoreCase);

    public static string Path(string key)
        => $"/leerling/puzzel-intro/{Uri.EscapeDataString(key)}";
}
