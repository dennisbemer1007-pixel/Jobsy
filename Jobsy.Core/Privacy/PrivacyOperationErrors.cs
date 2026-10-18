namespace Jobsy.Core.Privacy;

/// <summary>User-facing privacy operation failures with support reference codes.</summary>
public static class PrivacyOperationErrors
{
    public const string GenericMessageNl = "Er ging iets mis. Probeer het opnieuw.";

    public static string ShortReference(string? traceIdentifier)
    {
        if (string.IsNullOrWhiteSpace(traceIdentifier))
        {
            return "UNKNOWN";
        }

        var compact = traceIdentifier.Replace("-", "", StringComparison.Ordinal);
        return compact.Length >= 8
            ? compact[..8].ToUpperInvariant()
            : compact.ToUpperInvariant();
    }
}
