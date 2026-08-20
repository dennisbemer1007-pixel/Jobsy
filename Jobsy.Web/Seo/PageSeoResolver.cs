using Jobsy.Core;
using Jobsy.Core.Rules;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Seo;

/// <summary>Builds canonical/OG metadata from the route catalog plus an optional page overlay.</summary>
public static class PageSeoResolver
{
    public const int DescriptionMaxLength = 160;

    public static PageSeoModel Resolve(
        string navigationUri,
        CultureState culture,
        IConfiguration configuration,
        PageSeoOverride? overlay = null)
    {
        var path = AbsolutePath(navigationUri);
        var entry = PageSeoCatalog.Resolve(path);
        var indexable = overlay?.Indexable ?? entry.Indexable;

        var title = WithBrand(FirstNonEmpty(overlay?.Title, culture[entry.TitleKey], culture["Seo.SiteName"]) ?? "Lobsy");
        var description = HtmlSanitize.ToPlainPreview(
            FirstNonEmpty(overlay?.Description, culture[entry.DescriptionKey], culture["Seo.DefaultDescription"]),
            DescriptionMaxLength);

        var canonicalPath = overlay?.CanonicalPath;
        if (string.IsNullOrWhiteSpace(canonicalPath))
        {
            canonicalPath = path;
        }

        var canonical = CanonicalUrl(navigationUri, canonicalPath, configuration);
        var image = AbsoluteAssetUrl(
            FirstNonEmpty(overlay?.ImageUrl, BrandImages.AbsoluteWebp256),
            canonical);

        var ogType = FirstNonEmpty(overlay?.OgType, entry.OgType, "website")!;
        var robots = indexable ? "index,follow" : "noindex,nofollow";

        return new PageSeoModel(
            title,
            description,
            canonical,
            robots,
            indexable,
            ogType,
            image,
            overlay?.JsonLd);
    }

    public static string CanonicalUrl(string navigationUri, string? path, IConfiguration configuration)
    {
        var origin = Origin(navigationUri, configuration);
        var normalized = PageSeoCatalog.Normalize(path);
        return origin.TrimEnd('/') + (normalized == "/" ? "/" : normalized);
    }

    public static string Origin(string navigationUri, IConfiguration configuration)
    {
        if (Uri.TryCreate(navigationUri, UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.Host)
            && !CanonicalHost.IsLoopback(uri.Host))
        {
            var host = CanonicalHost.TryStripWww(uri.Host, out var apex) ? apex : uri.Host;
            var scheme = string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                ? Uri.UriSchemeHttps
                : uri.Scheme;
            return $"{scheme}://{host}";
        }

        var configured = JobsyPublicUrl.NormalizeOrigin(configuration["PublicWebBaseUrl"]);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        if (Uri.TryCreate(navigationUri, UriKind.Absolute, out uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : ":" + uri.Port)}";
        }

        return "https://lobsy.nl";
    }

    public static string AbsoluteAssetUrl(string? url, string canonicalPageUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return canonicalPageUrl;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (!Uri.TryCreate(canonicalPageUrl, UriKind.Absolute, out var page))
        {
            return trimmed;
        }

        return new Uri(page, trimmed.StartsWith('/') ? trimmed : "/" + trimmed).ToString();
    }

    public static string WithBrand(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Lobsy";
        }

        return title.Contains("Lobsy", StringComparison.OrdinalIgnoreCase)
            ? title.Trim()
            : title.Trim() + " · Lobsy";
    }

    private static string AbsolutePath(string navigationUri)
    {
        if (Uri.TryCreate(navigationUri, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath;
        }

        return PageSeoCatalog.Normalize(navigationUri);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
