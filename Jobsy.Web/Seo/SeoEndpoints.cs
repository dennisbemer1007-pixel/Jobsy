using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Features;
using Jobsy.Web.Services;

namespace Jobsy.Web.Seo;

public static class SitemapXml
{
    public const int MaxDynamicUrls = 5_000;

    public static string RobotsTxt(string origin, bool noIndex = false)
    {
        if (noIndex)
        {
            return "User-agent: *\nDisallow: /\n";
        }

        var sitemap = origin.TrimEnd('/') + "/sitemap.xml";
        return
            "User-agent: *\n" +
            "Allow: /\n" +
            "Disallow: /admin\n" +
            "Disallow: /employer\n" +
            "Disallow: /branch\n" +
            "Disallow: /candidate\n" +
            "Disallow: /home\n" +
            "Disallow: /salesmanager\n" +
            "Disallow: /ambassadeur\n" +
            "Disallow: /intermediary\n" +
            "Disallow: /regional\n" +
            "Disallow: /tokens\n" +
            "Disallow: /privacy/data\n" +
            "Disallow: /account\n" +
            "Disallow: /register/bedrijf\n" +
            "Disallow: /register/verifieren\n" +
            "Disallow: /register/toegang\n" +
            "Disallow: /register/koppelen\n" +
            "Disallow: /candidate/actions\n" +
            "Disallow: /werven\n" +
            "Disallow: /vestiging\n" +
            "\n" +
            "Sitemap: " + sitemap + "\n";
    }

    public static string Build(string origin, IReadOnlyList<string> paths, DateTimeOffset? lastmod = null)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        var stamp = (lastmod ?? DateTimeOffset.UtcNow).ToString("yyyy-MM-dd");
        var cap = MaxDynamicUrls + PageSeoCatalog.StaticIndexablePaths.Count;
        foreach (var path in paths)
        {
            if (seen.Count >= cap)
            {
                break;
            }

            var normalized = PageSeoCatalog.Normalize(path);
            if (!seen.Add(normalized) || !PageSeoCatalog.IsIndexable(normalized))
            {
                continue;
            }

            var loc = origin.TrimEnd('/') + (normalized == "/" ? "/" : normalized);
            sb.Append("  <url><loc>");
            sb.Append(System.Net.WebUtility.HtmlEncode(loc));
            sb.Append("</loc><changefreq>");
            sb.Append(normalized is "/" or "/banenkaart" ? "hourly" : "daily");
            sb.Append("</changefreq><lastmod>");
            sb.Append(stamp);
            sb.AppendLine("</lastmod></url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    public static string WeakETag(string content, bool employersEnabled)
    {
        var payload = Encoding.UTF8.GetBytes(content + "|" + (employersEnabled ? "on" : "zw"));
        var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        return $"W/\"{hash[..16]}\"";
    }
}

public sealed record SiteCrawlIndex(
    IReadOnlyList<SiteCrawlVacancy> Vacancies,
    IReadOnlyList<string> CompanyPaths);

public sealed record SiteCrawlVacancy(Guid Id, DateOnly StartDate, DateOnly EndDate);


public static class SeoEndpoints
{
    public static void MapSeoEndpoints(this WebApplication app)
    {
        app.MapGet("/robots.txt", async (IConfiguration config, HttpContext http, IFeatureFlags flags) =>
        {
            var origin = PageSeoResolver.Origin(
                $"{http.Request.Scheme}://{http.Request.Host}{http.Request.Path}",
                config);
            var snap = await flags.GetAsync(http.RequestAborted);
            var noIndex = config.GetValue<bool>("Seo:NoIndex");
            var robots = SitemapXml.RobotsTxt(origin, noIndex);
            if (!noIndex && !snap.EmployersEnabled)
            {
                robots = robots.Replace(
                    "Disallow: /vestiging\n",
                    "Disallow: /vestiging\nDisallow: /vacancies\nDisallow: /banen\nDisallow: /register\nDisallow: /westland\nDisallow: /lancering\n",
                    StringComparison.Ordinal);
            }

            // Short cache + variant-aware ETag so a flag flip reaches crawlers quickly (§S).
            http.Response.Headers.CacheControl = "public, max-age=300";
            http.Response.Headers.ETag = SitemapXml.WeakETag(robots, snap.EmployersEnabled);
            return Results.Text(robots, "text/plain; charset=utf-8");
        }).AllowAnonymous();

        app.MapGet("/sitemap.xml", async (
            IConfiguration config,
            HttpContext http,
            IHttpClientFactory clients,
            IFeatureFlags flags,
            CancellationToken cancellationToken) =>
        {
            if (config.GetValue<bool>("Seo:NoIndex"))
            {
                return Results.NotFound();
            }

            var origin = PageSeoResolver.Origin(
                $"{http.Request.Scheme}://{http.Request.Host}{http.Request.Path}",
                config);
            var snap = await flags.GetAsync(cancellationToken);
            var enabled = snap.EmployersEnabled;
            var paths = new List<string>(PageSeoCatalog.StaticIndexablePathsFor(snap));

            if (enabled)
            {
                try
                {
                    var client = clients.CreateClient("JobsySeo");
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var index = await client.GetFromJsonAsync<SiteCrawlIndex>(
                        "api/site/crawl-index",
                        jsonOptions,
                        cancellationToken);
                    if (index is not null)
                    {
                        foreach (var vacancy in index.Vacancies.Take(SitemapXml.MaxDynamicUrls))
                        {
                            paths.Add($"/vacancies/{vacancy.Id:D}");
                        }

                        foreach (var companyPath in index.CompanyPaths)
                        {
                            if (!string.IsNullOrWhiteSpace(companyPath)
                                && PageSeoCatalog.IsPublicCompanyPath(companyPath))
                            {
                                paths.Add(companyPath);
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
                {
                    // Static marketing URLs still help crawlers when the API is briefly unreachable.
                }
            }

            var body = SitemapXml.Build(origin, paths);
            http.Response.Headers.CacheControl = "public, max-age=300";
            http.Response.Headers.ETag = SitemapXml.WeakETag(body, enabled);
            return Results.Text(body, "application/xml; charset=utf-8");
        }).AllowAnonymous();

        app.MapGet("/.well-known/security.txt", async (
            HttpContext http,
            IConfiguration config,
            LegalIdentityProvider legal,
            CancellationToken cancellationToken) =>
        {
            var snap = await legal.GetAsync(cancellationToken);
            var origin = PageSeoResolver.Origin(
                $"{http.Request.Scheme}://{http.Request.Host}{http.Request.Path}",
                config);
            var body = SecurityTxt.Format(snap.PrivacyContact, origin, DateTime.UtcNow);
            http.Response.Headers.CacheControl = "public, max-age=3600";
            return Results.Text(body, "text/plain; charset=utf-8");
        }).AllowAnonymous();
    }
}

/// <summary>RFC 9116 security.txt. The Contact line is omitted when no mailbox is configured.</summary>
public static class SecurityTxt
{
    public static string Format(string? contactEmail, string origin, DateTime utcNow)
    {
        var expires = utcNow.AddMonths(12).ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            System.Globalization.CultureInfo.InvariantCulture);
        var body = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(contactEmail))
        {
            body.Append("Contact: mailto:").Append(contactEmail.Trim()).Append('\n');
        }

        body.Append("Expires: ").Append(expires).Append('\n');
        body.Append("Preferred-Languages: nl, en\n");
        body.Append("Canonical: ").Append(origin.TrimEnd('/')).Append("/.well-known/security.txt\n");
        return body.ToString();
    }
}
