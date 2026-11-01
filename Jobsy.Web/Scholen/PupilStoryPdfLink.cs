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
            parts.Add($"lang={Uri.EscapeDataString(lang)}");
        }

        if (parts.Count == 0)
        {
            return "/leerling/pdf";
        }

        return SanitizeHref($"/leerling/pdf?{string.Join("&", parts)}");
    }

    /// <summary>Removes stray query separators that showed up in the browser bar as <c>?lang=nl?</c>.</summary>
    public static string SanitizeHref(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return "/leerling/pdf";
        }

        var s = href.Trim();
        while (s.EndsWith('?'))
        {
            s = s[..^1];
        }

        s = s.Replace("?&", "?", StringComparison.Ordinal);
        s = s.Replace("&&", "&", StringComparison.Ordinal);
        var q = s.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0 && q < s.Length - 1)
        {
            var path = s[..q];
            var query = s[(q + 1)..].Trim('?');
            if (string.IsNullOrEmpty(query))
            {
                return path;
            }

            s = $"{path}?{query}";
        }

        return s;
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
