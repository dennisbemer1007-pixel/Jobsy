using Jobsy.Core.Enums;

namespace Jobsy.Core.Media;

/// <summary>
/// Resolves vacancy photos for list/detail/map. Stored http(s), <c>/images/…</c> and
/// data-URI values are kept after path cleanup. Empty or junk values fall through to
/// a company logo, then a local work-type fallback photo — never to the Lobsy brand mark.
/// Broken Unsplash / picsum URLs map to a local work-type photo when a vacancy id is known.
/// </summary>
public static class VacancyImageUrls
{
    public const int IntrinsicWidth = 800;
    public const int IntrinsicHeight = 533;
    public const int ListCardWidth = 400;
    public const string LocalPrefix = "/images/vacancies/";
    public const string ImagesPrefix = "/images/";
    public const string FallbackExtension = ".webp";
    public const string FallbackSrcSetWidth = "400";

    private static readonly string[] ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".avif", ".bmp"
    ];

    private static readonly string[] StorageFolders =
    [
        "uploads/", "logos/", "vacancies/", "brand/", "media/", "teaser/"
    ];

    public static string Placeholder(Guid vacancyId, WorkType workTypes = WorkType.None)
        => Placeholder(vacancyId, FirstSlug(workTypes));

    /// <summary>
    /// Category fallback photo (~800px WebP). <paramref name="vacancyId"/> is kept for
    /// call-site compatibility; one photo is used per work-type slug.
    /// </summary>
    public static string Placeholder(Guid vacancyId, string? workType)
    {
        _ = vacancyId;
        var slug = NormalizeSlug(workType);
        return $"{LocalPrefix}{slug}{FallbackExtension}";
    }

    /// <summary>400px companion for a primary fallback photo returned by <see cref="Placeholder"/>.</summary>
    public static string PlaceholderSrcSetCompanion(string placeholderUrl)
    {
        if (!IsLocalVacancyFallbackPhoto(placeholderUrl))
        {
            return placeholderUrl;
        }

        var slug = FallbackSlugFromUrl(placeholderUrl);
        return $"{LocalPrefix}{slug}-{FallbackSrcSetWidth}{FallbackExtension}";
    }

    /// <summary>Same-origin bytes endpoint so list JSON never embeds data-URIs.</summary>
    public static string PublicImagePath(Guid vacancyId)
        => vacancyId == Guid.Empty ? string.Empty : $"/api/vacancies/{vacancyId:D}/image";

    public static bool IsInlineDataUri(string? imageUrl)
        => !string.IsNullOrWhiteSpace(imageUrl)
           && imageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// List/map JSON: never ship Base64. Inline photos and third-party placeholders
    /// become a local work-type photo (or company logo via <see cref="Resolve"/>).
    /// Prefer <see cref="ForCard"/> for card/list/popup so inline photos and logos match detail.
    /// </summary>
    public static string? ForPublicList(string? imageUrl, Guid? vacancyId = null, string? workType = null)
    {
        var normalized = Normalize(imageUrl);
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
            || IsInlineDataUri(normalized)
            || IsPicsum(normalized)
            || IsBrokenUnsplash(normalized)
            || IsLocalVacancySvg(normalized))
        {
            return vacancyId is Guid id && id != Guid.Empty
                ? Placeholder(id, workType)
                : null;
        }

        return normalized;
    }

    /// <summary>
    /// Card / list / map popup thumbnail aligned with the detail page photo chain:
    /// inline data URI → same-origin <see cref="PublicImagePath"/>;
    /// usable same-origin <c>/images/…</c> photo → itself;
    /// else company logo (same-origin);
    /// else work-type fallback photo.
    /// <para>
    /// CSP choice: external absolute http(s) photo URLs are <b>not</b> returned
    /// (web <c>img-src 'self'</c>). They fall through to logo → photo instead of
    /// widening CSP to <c>https:</c>. Inline uploads stay available via
    /// <c>/api/vacancies/{id}/image</c>.
    /// </para>
    /// </summary>
    public static string ForCard(string? imageUrl, string? logoUrl, Guid id, string? workType)
    {
        if (id == Guid.Empty)
        {
            return Placeholder(Guid.Empty, workType);
        }

        var photo = Normalize(imageUrl);
        if (IsInlineDataUri(photo))
        {
            return PublicImagePath(id);
        }

        if (IsUsableSameOriginPhoto(photo))
        {
            return photo!;
        }

        // picsum / broken Unsplash / external https / empty → try logo
        var logo = Normalize(logoUrl);
        if (IsUsableSameOriginPhoto(logo))
        {
            return logo!;
        }

        return Placeholder(id, workType);
    }

    /// <summary>
    /// Card picture kind for UI styling: <c>photo</c>, <c>logo</c>, or <c>placeholder</c>.
    /// </summary>
    public static string ForCardKind(string pictureUrl, string? logoUrl)
    {
        var logo = Normalize(logoUrl);
        if (!string.IsNullOrWhiteSpace(logo)
            && string.Equals(pictureUrl, logo, StringComparison.OrdinalIgnoreCase))
        {
            return "logo";
        }

        if (IsLocalVacancyFallbackPhoto(pictureUrl)
            || IsLocalVacancySvg(pictureUrl)
            || (!string.IsNullOrWhiteSpace(pictureUrl)
                && pictureUrl.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            return "placeholder";
        }

        return "photo";
    }

    private static bool IsUsableSameOriginPhoto(string? normalized)
        => !string.IsNullOrWhiteSpace(normalized)
           && !normalized.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
           && !IsInlineDataUri(normalized)
           && !IsPicsum(normalized)
           && !IsBrokenUnsplash(normalized)
           && !IsLocalVacancySvg(normalized) // legacy category icons → Placeholder WebP
           && IsSafeSameOriginPath(normalized);

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
        var primary = UsableSource(imageUrl);
        if (primary is not null)
        {
            return primary;
        }

        var fallback = UsableSource(fallbackUrl);
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
            || resolved.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || IsLocalVacancyFallbackPhoto(resolved))
        {
            // Local category WebPs ship with their own 400/800 srcset; skip CF wrap.
            return resolved;
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
        var fallback = UsableSource(fallbackUrl);
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
        if (string.IsNullOrWhiteSpace(displayUrl)
            || displayUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || displayUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (IsLocalVacancyFallbackPhoto(displayUrl))
        {
            var at400 = PlaceholderSrcSetCompanion(displayUrl);
            return $"{at400} 400w, {displayUrl} 800w";
        }

        if (!cloudflareResizing
            || !displayUrl.Contains("/cdn-cgi/image/", StringComparison.Ordinal))
        {
            return null;
        }

        var at400Cdn = ReplaceWidth(displayUrl, 400);
        var at800Cdn = ReplaceWidth(displayUrl, 800);
        return $"{at400Cdn} 400w, {at800Cdn} 800w";
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

    /// <summary>Local category fallback WebP under <see cref="LocalPrefix"/> (primary or -400).</summary>
    public static bool IsLocalVacancyFallbackPhoto(string? imageUrl)
        => !string.IsNullOrWhiteSpace(imageUrl)
           && imageUrl.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase)
           && imageUrl.EndsWith(FallbackExtension, StringComparison.OrdinalIgnoreCase);

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

    private static string? UsableSource(string? imageUrl)
    {
        var normalized = Normalize(imageUrl);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        // Third-party / legacy SVG placeholders are discarded so Resolve can pick a local WebP.
        if (IsBrokenUnsplash(normalized) || IsPicsum(normalized) || IsLocalVacancySvg(normalized))
        {
            return null;
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
        if (t.Contains("tuinbouw", StringComparison.Ordinal)
            || t.Contains("groen", StringComparison.Ordinal)) return "tuinbouw";
        if (t.Contains("zorg", StringComparison.Ordinal)) return "zorg";
        if (t.Contains("kantoor", StringComparison.Ordinal)
            || t.Contains("administratie", StringComparison.Ordinal)) return "kantoor";
        if (t.Contains("bouw", StringComparison.Ordinal)
            || t.Contains("techniek", StringComparison.Ordinal)) return "bouw";
        if (t.Contains("schoonmaak", StringComparison.Ordinal)) return "schoonmaak";
        if (t.Contains("onderwijs", StringComparison.Ordinal)
            || t.Contains("education", StringComparison.Ordinal)) return "onderwijs";
        if (t.Contains("productie", StringComparison.Ordinal)) return "productie";
        if (t.Contains("overig", StringComparison.Ordinal)) return "flex";
        return "flex";
    }

    private static string FallbackSlugFromUrl(string url)
    {
        var file = url[LocalPrefix.Length..];
        var q = file.IndexOfAny(['?', '#']);
        if (q >= 0)
        {
            file = file[..q];
        }

        if (file.EndsWith(FallbackExtension, StringComparison.OrdinalIgnoreCase))
        {
            file = file[..^FallbackExtension.Length];
        }

        const string suffix400 = "-400";
        if (file.EndsWith(suffix400, StringComparison.OrdinalIgnoreCase))
        {
            file = file[..^suffix400.Length];
        }

        return string.IsNullOrWhiteSpace(file) ? "flex" : file;
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
