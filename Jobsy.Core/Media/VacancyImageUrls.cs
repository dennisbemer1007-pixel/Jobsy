using Jobsy.Core.Enums;

namespace Jobsy.Core.Media;

/// <summary>
/// Resolves vacancy photos for list/detail/map. Stored http(s), <c>/images/…</c> and
/// data-URI values are kept after path cleanup. Empty or junk values fall through to
/// a company logo, then a local work-type SVG — never to the Lobsy brand mark.
/// Broken Unsplash URLs map to a stable picsum seed when a vacancy id is known.
/// </summary>
public static class VacancyImageUrls
{
    public const int IntrinsicWidth = 600;
    public const int IntrinsicHeight = 400;
    public const int ListCardWidth = 400;
    public const string LocalPrefix = "/images/vacancies/";
    public const string ImagesPrefix = "/images/";
    public const int VariantCount = 2;

    private static readonly string[] ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".avif", ".bmp"
    ];

    private static readonly string[] StorageFolders =
    [
        "uploads/", "logos/", "vacancies/", "brand/", "media/", "teaser/"
    ];

    public static string PicsumUrl(Guid vacancyId)
        => PicsumUrl(vacancyId, IntrinsicWidth, IntrinsicHeight);

    public static string PicsumUrl(Guid vacancyId, int width, int height)
    {
        width = Math.Clamp(width, 80, IntrinsicWidth);
        height = Math.Clamp(height, 54, IntrinsicHeight);
        return $"https://picsum.photos/seed/jobsy-{vacancyId:N}/{width}/{height}";
    }

    public static string Placeholder(Guid vacancyId, WorkType workTypes = WorkType.None)
        => Placeholder(vacancyId, FirstSlug(workTypes));

    public static string Placeholder(Guid vacancyId, string? workType)
    {
        var slug = NormalizeSlug(workType);
        var variant = (int)(StableHash(vacancyId) % VariantCount);
        return $"{LocalPrefix}{slug}-{variant}.svg";
    }

    /// <summary>Same-origin bytes endpoint so list JSON never embeds data-URIs.</summary>
    public static string PublicImagePath(Guid vacancyId)
        => vacancyId == Guid.Empty ? string.Empty : $"/api/vacancies/{vacancyId:D}/image";

    public static bool IsInlineDataUri(string? imageUrl)
        => !string.IsNullOrWhiteSpace(imageUrl)
           && imageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// List/map JSON: never ship Base64. Inline photos become a cacheable
    /// <c>/api/vacancies/{id}/image</c> URL; picsum is downsized to card width.
    /// </summary>
    public static string? ForPublicList(string? imageUrl, Guid? vacancyId = null, string? workType = null)
    {
        var normalized = Normalize(imageUrl);
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return vacancyId is Guid id && id != Guid.Empty
                ? Placeholder(id, workType)
                : null;
        }

        if (IsInlineDataUri(normalized))
        {
            return vacancyId is Guid id && id != Guid.Empty
                ? Placeholder(id, workType)
                : null;
        }

        if (IsPicsum(normalized))
        {
            return SizedPicsum(normalized, ListCardWidth, vacancyId);
        }

        return normalized;
    }

    /// <summary>Decode a stored <c>data:image/…;base64,</c> photo for the public image endpoint.</summary>
    public static bool TryDecodeInlineImage(string? imageUrl, out byte[] bytes, out string contentType)
    {
        bytes = [];
        contentType = "image/jpeg";
        var raw = Normalize(imageUrl);
        if (!IsInlineDataUri(raw))
        {
            return false;
        }

        var comma = raw!.IndexOf(',');
        if (comma < 0 || comma + 1 >= raw.Length)
        {
            return false;
        }

        var header = raw[..comma];
        var payload = raw[(comma + 1)..].Replace(" ", "", StringComparison.Ordinal);
        var mimeEnd = header.IndexOf(';');
        var mime = mimeEnd > 5 ? header[5..mimeEnd] : header[5..];
        mime = mime.Trim().ToLowerInvariant();
        contentType = mime switch
        {
            "image/jpg" => "image/jpeg",
            "image/jpeg" or "image/png" or "image/gif" or "image/webp" => mime,
            _ => "application/octet-stream"
        };

        if (contentType == "application/octet-stream" || payload.Length == 0)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }

        if (bytes.Length is 0 or > Jobsy.Core.Rules.HtmlSanitize.MaxImageBytes)
        {
            bytes = [];
            return false;
        }

        return true;
    }

    /// <summary>
    /// Turns empty/null/"null", missing slashes, wwwroot prefixes and own-origin
    /// absolute URLs into a same-origin <c>/images/…</c> path (or keeps a remote URL).
    /// Returns null when there is no usable image source.
    /// </summary>
    public static string? Normalize(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var trimmed = imageUrl.Trim().Trim('"', '\'');
        if (trimmed.Length == 0
            || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("none", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("nil", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        trimmed = trimmed.Replace('\\', '/');
        if (trimmed.Contains("..", StringComparison.Ordinal)
            || trimmed.IndexOfAny(['\n', '\r', '\0']) >= 0)
        {
            return null;
        }

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return Normalize("https:" + trimmed);
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return ExtractImagesPath(uri.AbsolutePath) ?? ExtractImagesPath(trimmed);
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                return null;
            }

            if (IsOwnHost(uri.Host))
            {
                var path = uri.PathAndQuery;
                return string.IsNullOrWhiteSpace(path) || path == "/" ? null : path;
            }

            return uri.ToString();
        }

        return NormalizeRelative(trimmed);
    }

    public static string Resolve(string? imageUrl, Guid? vacancyId = null, string? workType = null)
        => Resolve(imageUrl, fallbackUrl: null, vacancyId, workType);

    public static string Resolve(string? imageUrl, Guid vacancyId, WorkType workTypes)
        => Resolve(imageUrl, fallbackUrl: null, vacancyId, FirstSlug(workTypes));

    public static string Resolve(
        string? imageUrl,
        string? fallbackUrl,
        Guid? vacancyId,
        string? workType)
    {
        var primary = UsableSource(imageUrl, vacancyId);
        if (primary is not null)
        {
            return primary;
        }

        var fallback = UsableSource(fallbackUrl, vacancyId: null);
        if (fallback is not null)
        {
            return fallback;
        }

        if (vacancyId is Guid id && id != Guid.Empty)
        {
            return Placeholder(id, workType);
        }

        return string.Empty;
    }

    public static string ForDisplay(
        string? imageUrl,
        int width,
        bool cloudflareResizing,
        Guid? vacancyId = null,
        string? workType = null)
        => ForDisplay(imageUrl, fallbackUrl: null, width, cloudflareResizing, vacancyId, workType);

    public static string ForDisplay(
        string? imageUrl,
        string? fallbackUrl,
        int width,
        bool cloudflareResizing,
        Guid? vacancyId = null,
        string? workType = null)
    {
        var resolved = Resolve(imageUrl, fallbackUrl, vacancyId, workType);
        if (string.IsNullOrWhiteSpace(resolved)
            || resolved.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || resolved.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return resolved;
        }

        if (IsPicsum(resolved))
        {
            return SizedPicsum(resolved, width, vacancyId);
        }

        if (!cloudflareResizing)
        {
            return resolved;
        }

        return IsSafeSameOriginPath(resolved) ? CdnResize(resolved, width) : resolved;
    }

    /// <summary>
    /// Alternate <c>src</c> for <c>onError</c>: company logo when it differs from the
    /// photo already chosen for display.
    /// </summary>
    public static string? AlternateSrc(string? imageUrl, string? fallbackUrl, string displaySrc)
    {
        var fallback = UsableSource(fallbackUrl, vacancyId: null);
        if (string.IsNullOrWhiteSpace(fallback)
            || string.Equals(fallback, displaySrc, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallback, Normalize(imageUrl), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fallback;
    }

    public static string? SrcSet(string displayUrl, bool cloudflareResizing)
    {
        if (!cloudflareResizing
            || string.IsNullOrWhiteSpace(displayUrl)
            || displayUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || displayUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || !displayUrl.Contains("/cdn-cgi/image/", StringComparison.Ordinal))
        {
            return null;
        }

        var at400 = ReplaceWidth(displayUrl, 400);
        var at800 = ReplaceWidth(displayUrl, 800);
        return $"{at400} 400w, {at800} 800w";
    }

    /// <summary>
    /// Cloudflare Image Resizing only for same-origin paths. Absolute http(s) URLs
    /// are not wrapped — that would let Cloudflare fetch attacker-controlled origins.
    /// </summary>
    public static string CdnResize(string url, int width)
    {
        if (!IsSafeSameOriginPath(url))
        {
            return url;
        }

        return $"/cdn-cgi/image/width={width},quality=75,format=auto{url}";
    }

    public static bool IsSafeSameOriginPath(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.StartsWith('/')
           && !url.StartsWith("//", StringComparison.Ordinal)
           && !url.Contains("..", StringComparison.Ordinal)
           && url.IndexOfAny(['\\', '\n', '\r', '\0']) < 0;

    public static bool IsLocalImagePath(string? url)
        => IsSafeSameOriginPath(url)
           && url!.StartsWith(ImagesPrefix, StringComparison.OrdinalIgnoreCase);

    public static bool IsPicsum(string? imageUrl)
        => !string.IsNullOrWhiteSpace(imageUrl)
           && imageUrl.Contains("picsum.photos", StringComparison.OrdinalIgnoreCase);

    public static bool IsBrokenUnsplash(string? imageUrl)
        => !string.IsNullOrWhiteSpace(imageUrl)
           && imageUrl.Contains("images.unsplash.com", StringComparison.OrdinalIgnoreCase);

    public static bool IsLocalVacancySvg(string? imageUrl)
        => !string.IsNullOrWhiteSpace(imageUrl)
           && imageUrl.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase)
           && imageUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

    public static bool IsThirdPartyPlaceholder(string? imageUrl)
        => IsBrokenUnsplash(imageUrl);

    public static string FirstSlug(WorkType workTypes)
    {
        foreach (var flag in new[]
                 {
                     WorkType.Horeca, WorkType.Winkel, WorkType.Logistiek, WorkType.Tuinbouw,
                     WorkType.Zorg, WorkType.Kantoor, WorkType.Bouw, WorkType.Schoonmaak,
                     WorkType.Productie
                 })
        {
            if (workTypes.HasFlag(flag))
            {
                return flag.ToString().ToLowerInvariant();
            }
        }

        return "flex";
    }

    private static string? UsableSource(string? imageUrl, Guid? vacancyId)
    {
        var normalized = Normalize(imageUrl);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (IsBrokenUnsplash(normalized))
        {
            var id = vacancyId ?? TryExtractSeed(imageUrl) ?? Guid.Empty;
            return id == Guid.Empty ? null : PicsumUrl(id);
        }

        return normalized;
    }

    private static string? NormalizeRelative(string path)
    {
        while (path.StartsWith("./", StringComparison.Ordinal))
        {
            path = path[2..];
        }

        const string wwwroot = "wwwroot/";
        if (path.StartsWith(wwwroot, StringComparison.OrdinalIgnoreCase))
        {
            path = path[wwwroot.Length..];
        }

        var extracted = ExtractImagesPath(path);
        if (extracted is not null)
        {
            path = extracted;
        }

        if (!path.StartsWith('/'))
        {
            if (path.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                path = "/" + path;
            }
            else if (IsStorageRelative(path))
            {
                path = ImagesPrefix + path.TrimStart('/');
            }
            else if (HasImageExtension(path) && path.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                path = ImagesPrefix + "uploads/" + path.TrimStart('/');
            }
            else
            {
                return null;
            }
        }

        while (path.Contains("//", StringComparison.Ordinal))
        {
            path = path.Replace("//", "/", StringComparison.Ordinal);
        }

        if (!IsSafeSameOriginPath(path))
        {
            return null;
        }

        return path;
    }

    private static string? ExtractImagesPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var idx = value.IndexOf("/images/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        var path = value[idx..];
        var hash = path.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0)
        {
            path = path[..hash];
        }

        return IsSafeSameOriginPath(path) ? path : null;
    }

    private static bool IsStorageRelative(string path)
    {
        foreach (var folder in StorageFolders)
        {
            if (path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasImageExtension(string path)
    {
        var cut = path.IndexOfAny(['?', '#']);
        var file = cut < 0 ? path : path[..cut];
        foreach (var ext in ImageExtensions)
        {
            if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOwnHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        host = host.Trim().TrimEnd('.');
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.Ordinal)
            || host.StartsWith("127.", StringComparison.Ordinal))
        {
            return true;
        }

        if (host.Equals("lobsy.nl", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".lobsy.nl", StringComparison.OrdinalIgnoreCase)
            || host.Equals("jobsy.local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".jobsy.local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeSlug(string? workType)
    {
        if (string.IsNullOrWhiteSpace(workType))
        {
            return "flex";
        }

        var t = workType.Trim().ToLowerInvariant();
        if (t.Contains("horeca", StringComparison.Ordinal)) return "horeca";
        if (t.Contains("winkel", StringComparison.Ordinal) || t.Contains("retail", StringComparison.Ordinal)) return "winkel";
        if (t.Contains("logistiek", StringComparison.Ordinal)) return "logistiek";
        if (t.Contains("tuinbouw", StringComparison.Ordinal)) return "tuinbouw";
        if (t.Contains("zorg", StringComparison.Ordinal)) return "zorg";
        if (t.Contains("kantoor", StringComparison.Ordinal)) return "kantoor";
        if (t.Contains("bouw", StringComparison.Ordinal)) return "bouw";
        if (t.Contains("schoonmaak", StringComparison.Ordinal)) return "schoonmaak";
        if (t.Contains("productie", StringComparison.Ordinal)) return "productie";
        return "flex";
    }

    private static string SizedPicsum(string url, int width, Guid? vacancyId)
    {
        var id = vacancyId ?? TryExtractSeed(url);
        if (id is null || id == Guid.Empty || width <= 0 || width >= IntrinsicWidth)
        {
            return url;
        }

        var height = Math.Max(54, (int)Math.Round(width * (IntrinsicHeight / (double)IntrinsicWidth)));
        return PicsumUrl(id.Value, width, height);
    }

    private static Guid? TryExtractSeed(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        const string marker = "jobsy-";
        var idx = imageUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        var start = idx + marker.Length;
        if (start + 32 > imageUrl.Length)
        {
            return null;
        }

        var hex = imageUrl.Substring(start, 32);
        return Guid.TryParseExact(hex, "N", out var id) ? id : null;
    }

    private static uint StableHash(Guid id)
    {
        var bytes = id.ToByteArray();
        unchecked
        {
            var hash = 2166136261;
            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= 16777619;
            }

            return hash;
        }
    }

    private static string ReplaceWidth(string cdnUrl, int width)
    {
        const string prefix = "width=";
        var start = cdnUrl.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            return cdnUrl;
        }

        var valueStart = start + prefix.Length;
        var valueEnd = valueStart;
        while (valueEnd < cdnUrl.Length && char.IsDigit(cdnUrl[valueEnd]))
        {
            valueEnd++;
        }

        return string.Concat(cdnUrl.AsSpan(0, valueStart), width.ToString(), cdnUrl.AsSpan(valueEnd));
    }
}
