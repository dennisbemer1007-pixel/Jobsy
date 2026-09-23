namespace Jobsy.Core.Rules;

/// <summary>
/// Outbound opleidingen must land on a specific course page — never a partner homepage.
/// </summary>
public static class TrainingDeepLinkRules
{
    public static string Combine(string baseUrl, string? externalPath)
    {
        if (string.IsNullOrWhiteSpace(externalPath))
        {
            throw new ArgumentException("Elke opleiding heeft een directe cursus-deeplink nodig (geen homepage).");
        }

        var path = externalPath.Trim();
        if (TryHttpAbsolute(path, out var absolute))
        {
            EnsureCourseDeepLink(absolute);
            return absolute.ToString();
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Opleider heeft geen basis-URL.");
        }

        var relative = path.TrimStart('/');
        if (relative.Length == 0)
        {
            throw new ArgumentException(
                "Deeplink mag niet naar de homepage van een opleider. Gebruik een directe cursus- of opleidingspagina.");
        }

        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/" + relative, UriKind.Absolute, out var combined)
            || combined.Scheme is not ("https" or "http"))
        {
            throw new ArgumentException("Ongeldige opleiders-URL.");
        }

        EnsureCourseDeepLink(combined);
        return combined.ToString();
    }

    public static bool TryCombine(string baseUrl, string? externalPath, out string url)
    {
        try
        {
            url = Combine(baseUrl, externalPath);
            return true;
        }
        catch (ArgumentException)
        {
            url = "";
            return false;
        }
    }

    public static bool IsCourseDeepLink(string url)
    {
        if (!TryHttpAbsolute(url, out var uri))
        {
            return false;
        }

        return !IsHomepageOnly(uri) && HasMeaningfulPath(uri);
    }

    public static bool IsHomepageOnly(Uri uri)
    {
        var path = uri.AbsolutePath.Trim();
        return string.IsNullOrEmpty(path) || path == "/";
    }

    public static void EnsureCourseDeepLink(Uri uri)
    {
        if (uri.Scheme is not ("https" or "http"))
        {
            throw new ArgumentException("Opleiders-URL moet http(s) zijn.");
        }

        if (IsHomepageOnly(uri) || !HasMeaningfulPath(uri))
        {
            throw new ArgumentException(
                "Deeplink mag niet naar de homepage van een opleider. Gebruik een directe cursus- of opleidingspagina.");
        }
    }

    public static string? NormalizeExternalPath(string? externalPath)
    {
        if (string.IsNullOrWhiteSpace(externalPath))
        {
            return null;
        }

        var trimmed = externalPath.Trim();
        if (TryHttpAbsolute(trimmed, out var absolute))
        {
            EnsureCourseDeepLink(absolute);
            return absolute.ToString();
        }

        var path = "/" + trimmed.TrimStart('/');
        if (path is "/" or "//")
        {
            return null;
        }

        return path;
    }

    private static bool TryHttpAbsolute(string value, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (parsed.Scheme is not ("https" or "http"))
        {
            // Paths like "/opleidingen/..." are absolute file URIs on Linux — treat as relative.
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool HasMeaningfulPath(Uri uri)
    {
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0 && !string.Equals(s, "index.html", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(s, "index.htm", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(s, "home", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return segments.Count >= 1 && segments.Any(s => s.Length >= 3);
    }
}
