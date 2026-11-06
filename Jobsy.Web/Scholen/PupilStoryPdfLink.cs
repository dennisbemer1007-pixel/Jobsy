using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Scholen;

/// <summary>Builds <c>/leerling/pdf</c> links without malformed query strings.</summary>
public static class PupilStoryPdfLink
{
    public const string Path = "/leerling/pdf";

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
            return Path;
        }

        return SanitizeHref($"{Path}?{string.Join("&", parts)}");
    }

    /// <summary>Rebuilds the canonical PDF URL from request query keys (fixes <c>?lang=nl?</c> in the address bar).</summary>
    public static string HrefFromQuery(IQueryCollection query)
    {
        query.TryGetValue("naam", out var naamValues);
        query.TryGetValue("lang", out var langValues);
        var naam = naamValues.Count > 0 ? naamValues[0] : null;
        var lang = langValues.Count > 0 ? langValues[0] : null;
        return Href(naam, lang);
    }

    public static bool NeedsCanonicalRedirect(HttpRequest request)
    {
        var canonical = HrefFromQuery(request.Query);

        var rawTarget = request.HttpContext.Features
            .Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>()?.RawTarget;
        if (!string.IsNullOrEmpty(rawTarget))
        {
            var rawPathAndQuery = rawTarget.Split(' ')[0];
            if (rawPathAndQuery.Length == 0 || rawPathAndQuery[0] != '/')
            {
                rawPathAndQuery = "/" + rawPathAndQuery;
            }

            var sanitizedRaw = SanitizeHref(rawPathAndQuery);
            if (!string.Equals(sanitizedRaw, canonical, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(sanitizedRaw, rawPathAndQuery, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var current = (request.Path.Value ?? Path) + (request.QueryString.Value ?? "");
        if (string.Equals(current, canonical, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var sanitized = SanitizeHref(current);
        return !string.Equals(sanitized, canonical, StringComparison.OrdinalIgnoreCase)
               || !string.Equals(sanitized, current, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Removes stray query separators that showed up in the browser bar as <c>?lang=nl?</c>.</summary>
    public static string SanitizeHref(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return Path;
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
