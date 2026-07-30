namespace Jobsy.Core.Rules;

/// <summary>
/// Converts known public video watch URLs into safe iframe embed URLs (YouTube / Vimeo).
/// </summary>
public static class VideoEmbed
{
    public static string? TryGetEmbedUrl(string? url)
    {
        var normalized = HtmlSanitize.NormalizeMediaUrl(url);
        if (normalized is null || !Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.Trim().ToLowerInvariant();
        if (host is "youtu.be")
        {
            var id = uri.AbsolutePath.Trim('/');
            return IsYouTubeId(id) ? $"https://www.youtube-nocookie.com/embed/{id}" : null;
        }

        if (host is "www.youtube.com" or "youtube.com" or "m.youtube.com" or "www.youtube-nocookie.com")
        {
            if (uri.AbsolutePath.StartsWith("/embed/", StringComparison.OrdinalIgnoreCase))
            {
                var id = uri.AbsolutePath["/embed/".Length..].Trim('/');
                return IsYouTubeId(id) ? $"https://www.youtube-nocookie.com/embed/{id}" : null;
            }

            if (uri.AbsolutePath.StartsWith("/shorts/", StringComparison.OrdinalIgnoreCase))
            {
                var id = uri.AbsolutePath["/shorts/".Length..].Trim('/');
                return IsYouTubeId(id) ? $"https://www.youtube-nocookie.com/embed/{id}" : null;
            }

            var watchId = GetQueryValue(uri.Query, "v");
            return IsYouTubeId(watchId) ? $"https://www.youtube-nocookie.com/embed/{watchId}" : null;
        }

        if (host is "vimeo.com" or "www.vimeo.com" or "player.vimeo.com")
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var id = segments.LastOrDefault(s => s.All(char.IsDigit));
            return !string.IsNullOrWhiteSpace(id) ? $"https://player.vimeo.com/video/{id}" : null;
        }

        return null;
    }

    public static string? TryGetSafeWatchUrl(string? url)
        => HtmlSanitize.NormalizeMediaUrl(url);

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var trimmed = query.TrimStart('?');
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            var name = idx < 0 ? part : part[..idx];
            if (!name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return idx < 0 ? "" : Uri.UnescapeDataString(part[(idx + 1)..]);
        }

        return null;
    }

    private static bool IsYouTubeId(string? id)
        => !string.IsNullOrWhiteSpace(id)
           && id.Length is >= 8 and <= 20
           && id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
}
