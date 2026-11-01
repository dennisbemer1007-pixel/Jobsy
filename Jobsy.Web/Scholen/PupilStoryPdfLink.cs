namespace Jobsy.Web.Scholen;

/// <summary>Builds <c>/leerling/pdf</c> links without malformed query strings.</summary>
public static class PupilStoryPdfLink
{
    public static string Href(string? displayName, string? languageCode)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            parts.Add($"naam={Uri.EscapeDataString(displayName.Trim())}");
        }

        var lang = NormalizeLanguageCode(languageCode);
        if (!string.IsNullOrWhiteSpace(lang))
        {
            lang = lang.TrimEnd('?');
            parts.Add($"lang={Uri.EscapeDataString(lang)}");
        }

        if (parts.Count == 0)
        {
            return "/leerling/pdf";
        }

        var href = $"/leerling/pdf?{string.Join("&", parts)}";
        return href.TrimEnd('?');
    }

    public static string? NormalizeLanguageCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var s = raw.Trim().Trim('?').Trim();
        if (s.StartsWith("lang=", StringComparison.OrdinalIgnoreCase))
        {
            s = s["lang=".Length..].Trim().Trim('?').Trim();
        }

        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
