namespace Jobsy.Web.Scholen;

/// <summary>
/// Dutch text a school or teacher page may show after an action. Never the raw response body.
/// </summary>
public static class PortalActionError
{
    public const string Fallback = "Dat lukte niet. Probeer het opnieuw.";

    public static string From(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Fallback;
        }

        var text = message.Trim();
        if (text.StartsWith('{')
            || text.StartsWith('[')
            || text.Contains("supportCode", StringComparison.OrdinalIgnoreCase)
            || text.Contains("traceId", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("API call failed", StringComparison.OrdinalIgnoreCase)
            || text.Length > 400)
        {
            return Fallback;
        }

        return text;
    }
}
