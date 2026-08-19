using Jobsy.Core.Enums;

namespace Jobsy.Core.Media;

/// <summary>
/// Resolves vacancy photos for list/detail/map. Mock vacancies use stable
/// picsum seeds (unique photo per id). Broken Unsplash URLs and the temporary
/// local SVG stand-ins are mapped back to that seed.
/// </summary>
public static class VacancyImageUrls
{
    public const int IntrinsicWidth = 600;
    public const int IntrinsicHeight = 400;
    public const string LocalPrefix = "/images/vacancies/";
    public const int VariantCount = 2;

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

    public static string Resolve(string? imageUrl, Guid? vacancyId = null, string? workType = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)
            || IsBrokenUnsplash(imageUrl)
            || IsLocalVacancySvg(imageUrl))
        {
            var id = vacancyId ?? TryExtractSeed(imageUrl) ?? Guid.Empty;
            return id == Guid.Empty ? (imageUrl ?? "").Trim() : PicsumUrl(id);
        }

        return imageUrl.Trim();
    }

    public static string Resolve(string? imageUrl, Guid vacancyId, WorkType workTypes)
        => Resolve(imageUrl, vacancyId, FirstSlug(workTypes));

    public static string ForDisplay(
        string? imageUrl,
        int width,
        bool cloudflareResizing,
        Guid? vacancyId = null,
        string? workType = null)
    {
        var resolved = Resolve(imageUrl, vacancyId, workType);
        if (resolved.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
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
           && url.IndexOfAny(['\\', '\n', '\r']) < 0;

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
