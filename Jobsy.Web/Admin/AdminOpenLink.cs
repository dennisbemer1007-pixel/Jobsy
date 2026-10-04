namespace Jobsy.Web.Admin;

/// <summary>
/// Deep links such as <c>?open=</c> must not navigate again when the address already matches.
/// A same-URL <c>NavigateTo</c> during the first render becomes an HTTP redirect loop.
/// </summary>
public static class AdminOpenLink
{
    public static bool ShouldNavigate(string? currentUri, string target, bool interactive)
    {
        if (!interactive || string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        return !Same(currentUri, target);
    }

    public static bool Same(string? currentUri, string target)
    {
        var current = Normalize(currentUri);
        var next = Normalize(target);
        return string.Equals(current, next, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return "";
        }

        var text = uri.Trim();
        if (Uri.TryCreate(text, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            text = absolute.PathAndQuery;
        }

        var hash = text.IndexOf('#');
        if (hash >= 0)
        {
            text = text[..hash];
        }

        if (!text.StartsWith('/'))
        {
            text = "/" + text;
        }

        var q = text.IndexOf('?');
        var path = q >= 0 ? text[..q] : text;
        var query = q >= 0 ? text[(q + 1)..] : "";
        path = path.TrimEnd('/');
        if (path.Length == 0)
        {
            path = "/";
        }

        if (query.Length == 0)
        {
            return path;
        }

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2 && p[0].Length > 0)
            .Select(p => (Key: Uri.UnescapeDataString(p[0]), Value: Uri.UnescapeDataString(p[1])))
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Value, StringComparer.Ordinal)
            .Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value));
        return path + "?" + string.Join("&", pairs);
    }
}
