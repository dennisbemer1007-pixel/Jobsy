using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Polls whitelisted direct-employer career sites with AngleSharp (no agencies / aggregators).
/// </summary>
public sealed class AtsScrapeService : IAtsScrapeService
{
    public const string HttpClientName = "AtsScraper";

    private static readonly Regex VacancyPathHint = new(
        @"vacature|vacatures|job|jobs|werkenbij|carri[eè]re|career|sollicit",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PostalCodeHint = new(
        @"\b([1-9]\d{3})\s?[A-Za-z]{2}\b",
        RegexOptions.Compiled);

    private static readonly Regex HoursHint = new(
        @"(\d{1,2}(?:[.,]\d)?)\s*(?:-|t/m|tot)\s*(\d{1,2}(?:[.,]\d)?)\s*uur",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SalaryEuroHint = new(
        @"€\s*(\d{1,3}(?:[.,]\d{2})?)\s*(?:per\s*)?(?:uur|/u)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AtsScrapeService> _logger;

    public AtsScrapeService(
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<AtsScrapeService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<int> ScrapeAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _db.AtsScrapeSources.AsNoTracking()
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.Name)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var total = 0;
        foreach (var id in ids)
        {
            total += await ScrapeSourceAsync(id, cancellationToken);
        }

        return total;
    }

    public async Task<int> ScrapeSourceAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var source = await _db.AtsScrapeSources
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);
        if (source is null || !source.IsEnabled)
        {
            return 0;
        }

        if (AtsBlacklistFilter.IsBlockedHost(source.ListUrl)
            || AtsBlacklistFilter.IsBlocked(source.ListUrl, source.Name, source.Name))
        {
            _logger.LogWarning("ATS source {Name} blocked by blacklist; disabling.", source.Name);
            source.IsEnabled = false;
            await _db.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        string listHtml;
        try
        {
            listHtml = await FetchHtmlAsync(client, source.ListUrl, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ATS list fetch failed for {Domain}", source.Domain);
            source.LastScrapedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var detailUrls = ExtractDetailUrls(listHtml, source.ListUrl, source.Domain)
            .Take(40)
            .ToList();

        // Fallback: treat the list URL itself as a single vacancy page.
        if (detailUrls.Count == 0)
        {
            detailUrls.Add(source.ListUrl);
        }

        var upserted = 0;
        foreach (var detailUrl in detailUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (AtsBlacklistFilter.IsBlocked(detailUrl, null)
                || !AtsBlacklistFilter.IsDomainAllowed(detailUrl, source.Domain))
            {
                continue;
            }

            try
            {
                var html = string.Equals(detailUrl, source.ListUrl, StringComparison.OrdinalIgnoreCase)
                    ? listHtml
                    : await FetchHtmlAsync(client, detailUrl, cancellationToken);

                if (await UpsertFromHtmlAsync(source, detailUrl, html, cancellationToken))
                {
                    upserted++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "ATS detail scrape skipped for {Url}", detailUrl);
            }
        }

        source.LastScrapedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "ATS scrape {Name}: upserted={Upserted} from {Urls} urls.",
            source.Name, upserted, detailUrls.Count);
        return upserted;
    }

    private async Task<bool> UpsertFromHtmlAsync(
        AtsScrapeSource source,
        string sourceUrl,
        string html,
        CancellationToken cancellationToken)
    {
        var parser = new HtmlParser();
        using var document = await parser.ParseDocumentAsync(html, cancellationToken);

        var title = FirstNonEmpty(
            MetaContent(document, "og:title"),
            document.QuerySelector("h1")?.TextContent,
            document.Title);
        title = CleanText(title);
        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
        {
            return false;
        }

        var companyName = FirstNonEmpty(
            MetaContent(document, "og:site_name"),
            source.Name);
        companyName = CleanText(companyName) ?? source.Name;

        if (AtsBlacklistFilter.IsBlocked(sourceUrl, title, companyName))
        {
            return false;
        }

        var description = ExtractDescription(document);
        var locationLabel = FirstNonEmpty(
            ExtractLocation(document, description),
            source.DefaultLocationLabel);
        var postal = ExtractPostal(description) ?? ExtractPostal(locationLabel);
        var locationKey = FirstNonEmpty(postal, locationLabel);

        var salaryText = ExtractSalaryText(description);
        var hourly = ParseHourly(salaryText) ?? ParseHourly(description);
        var (minH, maxH, hoursText) = ParseHours(description);
        var imageUrl = MetaContent(document, "og:image");
        if (!string.IsNullOrWhiteSpace(imageUrl)
            && imageUrl.Length > HtmlSanitize.MaxImageUrlLength)
        {
            imageUrl = null;
        }

        var tags = ExtractTags(document, description);
        var tagsJson = tags.Count > 0 ? JsonSerializer.Serialize(tags) : null;

        var hash = AtsDedupeHash.Compute(companyName, title, locationKey);
        var now = DateTime.UtcNow;
        var existing = await _db.AtsScrapedListings
            .FirstOrDefaultAsync(l => l.DedupHash == hash, cancellationToken);

        var completeness = AtsCompletenessScore.Compute(
            title, companyName, locationLabel, description,
            salaryText, hourly, hoursText, minH, maxH, tagsJson, sourceUrl);

        if (existing is null)
        {
            _db.AtsScrapedListings.Add(new AtsScrapedListing
            {
                Id = Guid.NewGuid(),
                SourceId = source.Id,
                DedupHash = hash,
                SourceUrl = Truncate(sourceUrl, 2048) ?? sourceUrl,
                CompanyName = Truncate(companyName, 256)!,
                Title = Truncate(title, 256)!,
                LocationLabel = Truncate(locationLabel, 256),
                PostalCode = Truncate(postal, 16),
                Description = Truncate(description, 20_000) ?? string.Empty,
                SalaryText = Truncate(salaryText, 512),
                HourlyWage = hourly,
                HoursText = Truncate(hoursText, 256),
                MinHoursPerWeek = minH,
                MaxHoursPerWeek = maxH,
                TagsJson = Truncate(tagsJson, 2000),
                ImageUrl = imageUrl,
                Latitude = source.DefaultLatitude,
                Longitude = source.DefaultLongitude,
                CompletenessScore = completeness,
                Status = AtsListingStatus.PendingReview,
                ScrapedAtUtc = now,
                LastCheckedAtUtc = now,
                ExpiresAtUtc = AtsVacancyRules.DefaultExpiresAt(now)
            });
            return true;
        }

        // Refresh content for non-terminal statuses; keep Approved/Rejected as-is for status.
        existing.SourceUrl = Truncate(sourceUrl, 2048) ?? sourceUrl;
        existing.SourceId = source.Id;
        existing.CompanyName = Truncate(companyName, 256)!;
        existing.Title = Truncate(title, 256)!;
        existing.LocationLabel = Truncate(locationLabel, 256);
        existing.PostalCode = Truncate(postal, 16);
        existing.Description = Truncate(description, 20_000) ?? string.Empty;
        existing.SalaryText = Truncate(salaryText, 512);
        existing.HourlyWage = hourly;
        existing.HoursText = Truncate(hoursText, 256);
        existing.MinHoursPerWeek = minH;
        existing.MaxHoursPerWeek = maxH;
        existing.TagsJson = Truncate(tagsJson, 2000);
        existing.ImageUrl = imageUrl;
        existing.CompletenessScore = completeness;
        existing.LastCheckedAtUtc = now;
        if (existing.Status is AtsListingStatus.Expired or AtsListingStatus.Inactive)
        {
            existing.Status = AtsListingStatus.PendingReview;
            existing.ExpiresAtUtc = AtsVacancyRules.DefaultExpiresAt(now);
            existing.ScrapedAtUtc = now;
            existing.RejectReason = null;
        }

        return true;
    }

    internal static IReadOnlyList<string> ExtractDetailUrls(string html, string listUrl, string allowedDomain)
    {
        var parser = new HtmlParser();
        using var document = parser.ParseDocument(html);
        if (!Uri.TryCreate(listUrl, UriKind.Absolute, out var baseUri))
        {
            return [];
        }

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href)
                || href.StartsWith('#')
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!Uri.TryCreate(baseUri, href, out var absolute))
            {
                continue;
            }

            if (absolute.Scheme is not ("http" or "https"))
            {
                continue;
            }

            if (!AtsBlacklistFilter.IsDomainAllowed(absolute.AbsoluteUri, allowedDomain))
            {
                continue;
            }

            var path = absolute.AbsolutePath;
            var text = anchor.TextContent ?? string.Empty;
            var looksLikeVacancy = VacancyPathHint.IsMatch(path)
                                   || VacancyPathHint.IsMatch(text)
                                   || VacancyPathHint.IsMatch(absolute.AbsoluteUri);
            if (!looksLikeVacancy)
            {
                continue;
            }

            // Prefer detail-ish paths over the listing root itself.
            var normalized = absolute.GetLeftPart(UriPartial.Query);
            if (string.Equals(normalized.TrimEnd('/'), listUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            found.Add(normalized);
        }

        return found.OrderBy(u => u, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static async Task<string> FetchHtmlAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string ExtractDescription(IDocument document)
    {
        var meta = MetaContent(document, "og:description")
                   ?? MetaContent(document, "description");
        var main = document.QuerySelector("main")
                   ?? document.QuerySelector("article")
                   ?? document.QuerySelector("[itemprop=description]")
                   ?? document.Body;
        var bodyText = CleanText(main?.TextContent);
        if (!string.IsNullOrWhiteSpace(bodyText) && bodyText.Length >= 80)
        {
            return bodyText;
        }

        return CleanText(meta) ?? bodyText ?? string.Empty;
    }

    private static string? ExtractLocation(IDocument document, string description)
    {
        var item = document.QuerySelector("[itemprop=jobLocation], [itemprop=addressLocality], .location, .vacancy-location");
        var fromDom = CleanText(item?.TextContent);
        if (!string.IsNullOrWhiteSpace(fromDom))
        {
            return fromDom;
        }

        var postal = ExtractPostal(description);
        return postal;
    }

    private static string? ExtractPostal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var m = PostalCodeHint.Match(text);
        return m.Success ? m.Value.Replace(" ", string.Empty).ToUpperInvariant() : null;
    }

    private static string? ExtractSalaryText(string description)
    {
        var m = SalaryEuroHint.Match(description);
        return m.Success ? m.Value.Trim() : null;
    }

    private static decimal? ParseHourly(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var m = SalaryEuroHint.Match(text);
        if (!m.Success)
        {
            return null;
        }

        var raw = m.Groups[1].Value.Replace(',', '.');
        return decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : null;
    }

    private static (decimal? Min, decimal? Max, string? Text) ParseHours(string description)
    {
        var m = HoursHint.Match(description);
        if (!m.Success)
        {
            return (null, null, null);
        }

        var minRaw = m.Groups[1].Value.Replace(',', '.');
        var maxRaw = m.Groups[2].Value.Replace(',', '.');
        decimal? min = decimal.TryParse(minRaw, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var a)
            ? a
            : null;
        decimal? max = decimal.TryParse(maxRaw, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var b)
            ? b
            : null;
        return (min, max, m.Value.Trim());
    }

    private static List<string> ExtractTags(IDocument document, string description)
    {
        var tags = new List<string>();
        foreach (var el in document.QuerySelectorAll(".tag, .label, .badge, [rel=tag]"))
        {
            var t = CleanText(el.TextContent);
            if (!string.IsNullOrWhiteSpace(t) && t.Length <= 40 && tags.Count < 8)
            {
                tags.Add(t);
            }
        }

        if (tags.Count == 0 && description.Contains("parttime", StringComparison.OrdinalIgnoreCase))
        {
            tags.Add("parttime");
        }

        return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? MetaContent(IDocument document, string nameOrProperty)
    {
        var el = document.QuerySelector($"meta[property='{nameOrProperty}']")
                 ?? document.QuerySelector($"meta[name='{nameOrProperty}']");
        return CleanText(el?.GetAttribute("content"));
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
    }

    private static string? Truncate(string? value, int max)
    {
        if (value is null)
        {
            return null;
        }

        return value.Length <= max ? value : value[..max];
    }
}
