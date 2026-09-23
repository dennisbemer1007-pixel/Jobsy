using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    /// <summary>Max list/hub/pagination pages crawled per whitelist domain.</summary>
    public const int MaxListPagesPerDomain = 25;

    /// <summary>Max unique vacancy detail URLs processed per domain.</summary>
    public const int MaxVacancyUrlsPerDomain = 200;

    /// <summary>Parallel HTML fetches per batch (DB upserts stay serial).</summary>
    public const int DetailFetchBatchSize = 5;

    private static readonly Regex VacancyPathHint = new(
        @"vacancy|vacancies|vacature|vacatures|job|jobs|werken[\-_]?bij|werkenbij|carri[eèé]re|career|careers|sollicit|openstaande[\-_]?funct|functie|recruiting|opportunities|stellenangebote",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PaginationPathHint = new(
        @"([?&](page|p|pagina|pg|start|offset|paged)=\d+)|(/page/\d+(/|$))|(/p/\d+(/|$))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PaginationTextHint = new(
        @"^\s*(volgende|next|meer|meer vacatures|volgende pagina|older|→|>|»|›)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ListHubPathHint = new(
        @"/(vacatures|vacancies|jobs|job|careers|career|werken[\-_]?bij|werkenbij|carri[eèé]re|sollicitat)/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Noise paths that are almost never vacancy detail pages.</summary>
    private static readonly Regex NonVacancyPathNoise = new(
        @"/(contact|privacy|cookie|login|account|cart|winkelwagen|checkout|nieuws|blog|over[\-_]?ons|about|faq|home)/?$|" +
        @"\.(pdf|jpg|jpeg|png|gif|svg|webp|css|js|zip|docx?|xlsx?)(\?|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>CSS-ish structural hooks used first; empty → keyword fallback.</summary>
    private static readonly string[] StructuralVacancySelectors =
    [
        ".job-item a[href]",
        ".job-listing a[href]",
        ".job-card a[href]",
        ".vacancy a[href]",
        ".vacature a[href]",
        ".vacatures a[href]",
        "[class*='job-item'] a[href]",
        "[class*='job-listing'] a[href]",
        "[class*='vacancy'] a[href]",
        "[class*='vacature'] a[href]",
        "[data-job] a[href]",
        "a[href*='vacature']",
        "a[href*='vacancy']",
        "a[href*='/jobs/']",
        "a[href*='/job/']",
        "a[href*='werken-bij']",
        "a[href*='werkenbij']",
        "a[href*='carriere']",
        "a[href*='career']"
    ];

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

    public async Task<AtsScrapeRunReport> ScrapeAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        var report = new AtsScrapeRunReport { StartedAtUtc = DateTime.UtcNow };
        Line(report, $"ATS scrape-all start at {report.StartedAtUtc:O}");
        _logger.LogInformation("ATS scrape-all starting.");

        var ids = await _db.AtsScrapeSources.AsNoTracking()
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.Name)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        report.SourceCount = ids.Count;
        Line(report, $"Enabled whitelist sources: {ids.Count}");

        foreach (var id in ids)
        {
            var part = await ScrapeSourceAsync(id, cancellationToken);
            Merge(report, part);
        }

        report.FinishedAtUtc = DateTime.UtcNow;
        Line(report,
            $"ATS scrape-all done: upserted={report.Upserted} inserted={report.Inserted} updated={report.Updated} " +
            $"dupHash={report.SkippedDuplicateHash} blacklist={report.SkippedBlacklist} parseSkip={report.SkippedParse} " +
            $"invalid={report.SkippedInvalid} httpErr={report.HttpErrors} failedSources={report.FailedSources}");
        _logger.LogInformation(
            "ATS scrape-all finished upserted={Upserted} inserted={Inserted} updated={Updated} httpErrors={HttpErrors} failedSources={Failed}",
            report.Upserted, report.Inserted, report.Updated, report.HttpErrors, report.FailedSources);
        return report;
    }

    public async Task<AtsScrapeRunReport> ScrapeSourceAsync(
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        var report = new AtsScrapeRunReport { StartedAtUtc = DateTime.UtcNow, SourceCount = 1 };
        var source = await _db.AtsScrapeSources
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);
        if (source is null)
        {
            Line(report, $"Source {sourceId} not found.");
            _logger.LogWarning("ATS scrape source {SourceId} not found.", sourceId);
            report.FinishedAtUtc = DateTime.UtcNow;
            return report;
        }

        var srcReport = new AtsScrapeSourceReport
        {
            SourceId = source.Id,
            Name = source.Name,
            Domain = source.Domain,
            ListUrl = source.ListUrl
        };
        report.Sources.Add(srcReport);

        Line(report, srcReport, $"START scrape domain={source.Domain} listUrl={source.ListUrl} enabled={source.IsEnabled}");
        _logger.LogInformation(
            "ATS scrape START name={Name} domain={Domain} url={Url}",
            source.Name, source.Domain, source.ListUrl);

        if (!source.IsEnabled)
        {
            srcReport.Error = "Bron is uitgeschakeld.";
            Line(report, srcReport, "SKIP: source disabled.");
            report.FinishedAtUtc = DateTime.UtcNow;
            return report;
        }

        if (AtsBlacklistFilter.IsBlockedHost(source.ListUrl)
            || AtsBlacklistFilter.IsBlocked(source.ListUrl, source.Name, source.Name))
        {
            srcReport.Error = "Bron geblokkeerd door blacklist.";
            Line(report, srcReport, "SKIP: blacklist hit on source; disabling.");
            _logger.LogWarning("ATS source {Name} blocked by blacklist; disabling.", source.Name);
            source.IsEnabled = false;
            await _db.SaveChangesAsync(cancellationToken);
            report.SkippedBlacklist++;
            srcReport.SkippedBlacklist++;
            report.FinishedAtUtc = DateTime.UtcNow;
            return report;
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var vacancyUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var listQueue = new Queue<string>();
        var visitedListPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingListPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var startKey = NormalizeUrlKey(source.ListUrl);
        listQueue.Enqueue(startKey);
        pendingListPages.Add(startKey);
        string? startListHtml = null;
        var totalRawAnchors = 0;
        var strategies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (listQueue.Count > 0
               && visitedListPages.Count < MaxListPagesPerDomain
               && vacancyUrls.Count < MaxVacancyUrlsPerDomain)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var listUrl = listQueue.Dequeue();
            pendingListPages.Remove(listUrl);
            if (!visitedListPages.Add(listUrl))
            {
                continue;
            }

            string listHtml;
            try
            {
                var (status, html) = await FetchHtmlAsync(client, listUrl, cancellationToken);
                if (srcReport.ListHttpStatus is null)
                {
                    srcReport.ListHttpStatus = (int)status;
                }

                srcReport.PagesScanned++;
                if (!string.Equals(listUrl, NormalizeUrlKey(source.ListUrl), StringComparison.OrdinalIgnoreCase))
                {
                    srcReport.PaginationFollowed++;
                }

                Line(report, srcReport,
                    $"LIST page #{srcReport.PagesScanned} HTTP {(int)status} {status} {listUrl}");
                _logger.LogInformation(
                    "ATS list page domain={Domain} page={Page} status={Status} url={Url}",
                    source.Domain, srcReport.PagesScanned, (int)status, listUrl);
                listHtml = html;
                if (startListHtml is null)
                {
                    startListHtml = html;
                }
            }
            catch (Exception ex) when (IsSkippableFetchFailure(ex))
            {
                if (srcReport.PagesScanned == 0 && vacancyUrls.Count == 0)
                {
                    await MarkSourceFailedAsync(report, srcReport, source, ex, cancellationToken);
                    return report;
                }

                srcReport.HttpErrors++;
                report.HttpErrors++;
                Line(report, srcReport,
                    $"SKIP list page ({ClassifyFetchFailure(ex)}): {listUrl} — {ex.Message}");
                continue;
            }

            var (pageVacancyUrls, rawAnchors, strategy) =
                ExtractDetailUrlsWithStats(listHtml, listUrl, source.Domain);
            totalRawAnchors += rawAnchors;
            strategies.Add(strategy);
            foreach (var u in pageVacancyUrls)
            {
                if (vacancyUrls.Count >= MaxVacancyUrlsPerDomain)
                {
                    break;
                }

                vacancyUrls.Add(u);
            }

            var nextPages = ExtractPaginationAndHubUrls(listHtml, listUrl, source.Domain).ToList();
            // Continue ?page=N only when this page actually yielded vacancy links.
            if (pageVacancyUrls.Count > 0
                && TryBuildNextPageUrl(listUrl, out var syntheticNext)
                && AtsBlacklistFilter.IsDomainAllowed(syntheticNext, source.Domain))
            {
                nextPages.Add(syntheticNext);
            }

            var enqueued = 0;
            foreach (var next in nextPages)
            {
                var key = NormalizeUrlKey(next);
                if (visitedListPages.Contains(key)
                    || pendingListPages.Contains(key)
                    || vacancyUrls.Contains(key))
                {
                    continue;
                }

                listQueue.Enqueue(key);
                pendingListPages.Add(key);
                enqueued++;
            }

            Line(report, srcReport,
                $"HARVEST page: strategy={strategy} rawAnchors={rawAnchors} " +
                $"vacancyLinks=+{pageVacancyUrls.Count} (unique total={vacancyUrls.Count}) " +
                $"pagination/hub enqueue={enqueued} queue={listQueue.Count}");
        }

        srcReport.RawAnchorCount = totalRawAnchors;
        srcReport.VacancyLinkCount = vacancyUrls.Count;
        Line(report, srcReport,
            $"CRAWL summary domain={source.Domain}: pagesScanned={srcReport.PagesScanned} " +
            $"paginationFollowed={srcReport.PaginationFollowed} uniqueVacancyUrls={vacancyUrls.Count} " +
            $"strategies={string.Join('|', strategies)}");
        _logger.LogInformation(
            "ATS crawl summary domain={Domain} pages={Pages} pagination={Pagination} uniqueUrls={Urls}",
            source.Domain, srcReport.PagesScanned, srcReport.PaginationFollowed, vacancyUrls.Count);

        var urls = vacancyUrls.Take(MaxVacancyUrlsPerDomain).ToList();
        if (urls.Count == 0)
        {
            urls.Add(source.ListUrl);
            Line(report, srcReport, "FALLBACK: no vacancy links — treating list URL as single detail page.");
            _logger.LogInformation("ATS fallback to list URL as detail for {Domain}", source.Domain);
        }

        Line(report, srcReport,
            $"DETAIL queue size={urls.Count} (batchSize={DetailFetchBatchSize}, cap={MaxVacancyUrlsPerDomain})");

        await ProcessDetailUrlsInBatchesAsync(
            client,
            source,
            urls,
            startListHtml,
            report,
            srcReport,
            cancellationToken);

        source.LastScrapedAtUtc = DateTime.UtcNow;
        FinalizeSourceStatus(report, srcReport);
        await _db.SaveChangesAsync(cancellationToken);

        srcReport.AtsListingsSaved = srcReport.Inserted;
        Line(report, srcReport,
            $"END {source.Name} status={srcReport.Status}: " +
            $"pagesScanned={srcReport.PagesScanned} uniqueVacancyUrls={srcReport.VacancyLinkCount} " +
            $"atsSaved={srcReport.AtsListingsSaved} refreshed={srcReport.Updated} upserted={srcReport.Upserted} " +
            $"dupHash={srcReport.SkippedDuplicateHash} blacklist={srcReport.SkippedBlacklist} " +
            $"parseSkip={srcReport.SkippedParse} invalid={srcReport.SkippedInvalid} httpErr={srcReport.HttpErrors}");
        _logger.LogInformation(
            "ATS scrape END name={Name} domain={Domain} status={Status} pages={Pages} uniqueUrls={Urls} " +
            "atsSaved={Inserted} refreshed={Updated} upserted={Upserted} blacklist={Blacklist} " +
            "parseSkip={Parse} invalid={Invalid} httpErr={Http}",
            source.Name, source.Domain, srcReport.Status, srcReport.PagesScanned, srcReport.VacancyLinkCount,
            srcReport.Inserted, srcReport.Updated, srcReport.Upserted, srcReport.SkippedBlacklist,
            srcReport.SkippedParse, srcReport.SkippedInvalid, srcReport.HttpErrors);

        report.FinishedAtUtc = DateTime.UtcNow;
        return report;
    }

    private async Task ProcessDetailUrlsInBatchesAsync(
        HttpClient client,
        AtsScrapeSource source,
        IReadOnlyList<string> urls,
        string? startListHtml,
        AtsScrapeRunReport report,
        AtsScrapeSourceReport srcReport,
        CancellationToken cancellationToken)
    {
        var startKey = NormalizeUrlKey(source.ListUrl);
        for (var offset = 0; offset < urls.Count; offset += DetailFetchBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = urls.Skip(offset).Take(DetailFetchBatchSize).ToList();
            Line(report, srcReport,
                $"DETAIL batch {offset / DetailFetchBatchSize + 1}: {batch.Count} URL(s) " +
                $"(progress {offset}/{urls.Count})");

            var fetchTasks = batch.Select(async detailUrl =>
            {
                if (AtsBlacklistFilter.IsBlocked(detailUrl, null)
                    || !AtsBlacklistFilter.IsDomainAllowed(detailUrl, source.Domain))
                {
                    return (detailUrl, Html: (string?)null, Skipped: "blacklist", Error: (Exception?)null);
                }

                try
                {
                    if (startListHtml is not null
                        && string.Equals(NormalizeUrlKey(detailUrl), startKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return (detailUrl, Html: startListHtml, Skipped: (string?)null, Error: (Exception?)null);
                    }

                    var (_, body) = await FetchHtmlAsync(client, detailUrl, cancellationToken);
                    return (detailUrl, Html: body, Skipped: (string?)null, Error: (Exception?)null);
                }
                catch (Exception ex) when (IsSkippableFetchFailure(ex))
                {
                    return (detailUrl, Html: (string?)null, Skipped: "http", Error: ex);
                }
            });

            var fetched = await Task.WhenAll(fetchTasks);

            // EF DbContext is not thread-safe — upsert serially after parallel fetch.
            foreach (var item in fetched)
            {
                if (item.Skipped == "blacklist")
                {
                    srcReport.SkippedBlacklist++;
                    report.SkippedBlacklist++;
                    Line(report, srcReport, $"SKIP blacklist/domain: {item.detailUrl}");
                    continue;
                }

                if (item.Skipped == "http" || item.Html is null)
                {
                    srcReport.HttpErrors++;
                    report.HttpErrors++;
                    var reason = item.Error is null ? "Error" : ClassifyFetchFailure(item.Error);
                    Line(report, srcReport,
                        $"SKIP failed URL ({reason}): {item.detailUrl} — {item.Error?.Message}");
                    _logger.LogInformation(
                        "ATS skip failed detail url={Url} reason={Reason}",
                        item.detailUrl, reason);
                    await WriteBackgroundLogAsync(
                        PlatformLogLevel.Warning,
                        $"Detail skip {source.Domain}: {reason} — {item.detailUrl}",
                        cancellationToken);
                    continue;
                }

                if (!string.Equals(NormalizeUrlKey(item.detailUrl), startKey, StringComparison.OrdinalIgnoreCase))
                {
                    srcReport.DetailPagesFetched++;
                }

                Line(report, srcReport, $"DETAIL parse {item.detailUrl}");
                var outcome = await UpsertFromHtmlAsync(source, item.detailUrl, item.Html, cancellationToken);
                ApplyOutcome(report, srcReport, outcome, item.detailUrl);
            }

            // Persist mid-domain so a long crawl does not hold everything in memory/change-tracker only.
            if (offset + DetailFetchBatchSize < urls.Count)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task MarkSourceFailedAsync(
        AtsScrapeRunReport report,
        AtsScrapeSourceReport srcReport,
        AtsScrapeSource source,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var reason = ClassifyFetchFailure(ex);
        srcReport.Status = "Failed";
        srcReport.HttpErrors++;
        report.HttpErrors++;
        report.FailedSources++;
        srcReport.Error = $"{reason}: {ex.Message}";
        srcReport.ListHttpStatus = TryParseStatus(ex.Message);
        Line(report, srcReport, $"FAILED domain={source.Domain} ({reason}): {ex.Message}");
        _logger.LogWarning(
            "ATS source FAILED name={Name} domain={Domain} reason={Reason}: {Message}",
            source.Name, source.Domain, reason, ex.Message);
        source.LastScrapedAtUtc = DateTime.UtcNow;
        await WriteBackgroundLogAsync(
            PlatformLogLevel.Warning,
            $"Source failed {source.Domain} ({reason}): {ex.Message}",
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        report.FinishedAtUtc = DateTime.UtcNow;
    }

    private static void FinalizeSourceStatus(AtsScrapeRunReport report, AtsScrapeSourceReport src)
    {
        if (string.Equals(src.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (src.HttpErrors > 0 && src.Upserted == 0)
        {
            src.Status = "Failed";
            report.FailedSources++;
            src.Error ??= "Geen geldige vacatures; HTTP/DNS-fouten op detail-URL's.";
        }
        else if (src.HttpErrors > 0 || src.SkippedInvalid > 0 || src.SkippedParse > 0)
        {
            src.Status = "Partial";
        }
        else
        {
            src.Status = "Ok";
        }
    }

    private async Task WriteBackgroundLogAsync(
        PlatformLogLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        _db.PlatformLogs.Add(new Core.Entities.PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = level,
            Category = "AtsScrape",
            Message = Truncate(message, 2000) ?? message,
            CreatedAt = DateTime.UtcNow
        });
        // Caller SaveChanges persists; for mid-run skips we still want durable logs.
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsSkippableFetchFailure(Exception ex)
    {
        if (ex is OperationCanceledException && ex is not TaskCanceledException)
        {
            return false;
        }

        return ex is HttpRequestException
               or TaskCanceledException
               or TimeoutException
               or System.Net.Sockets.SocketException
               || ex.InnerException is System.Net.Sockets.SocketException
               || (ex.Message?.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ?? false)
               || (ex.Message?.Contains("nodename nor servname", StringComparison.OrdinalIgnoreCase) ?? false)
               || (ex.Message?.Contains("No such host", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string ClassifyFetchFailure(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        if (msg.Contains("404", StringComparison.Ordinal) || msg.Contains("Gone", StringComparison.OrdinalIgnoreCase))
        {
            return "HTTP 404";
        }

        if (ex is TaskCanceledException or TimeoutException
            || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "Timeout";
        }

        if (ex is System.Net.Sockets.SocketException
            || ex.InnerException is System.Net.Sockets.SocketException
            || msg.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("No such host", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("nodename nor servname", StringComparison.OrdinalIgnoreCase))
        {
            return "DNS";
        }

        if (ex is HttpRequestException)
        {
            return "HTTP";
        }

        return "Error";
    }

    private enum UpsertOutcome
    {
        Inserted,
        Updated,
        DuplicateUnchanged,
        SkippedBlacklist,
        SkippedParse,
        SkippedInvalid
    }

    private async Task<UpsertOutcome> UpsertFromHtmlAsync(
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
            _logger.LogInformation("ATS parse skip (no title) {Url}", sourceUrl);
            return UpsertOutcome.SkippedParse;
        }

        var companyName = FirstNonEmpty(
            MetaContent(document, "og:site_name"),
            source.Name);
        companyName = CleanText(companyName) ?? source.Name;

        if (AtsBlacklistFilter.IsBlocked(sourceUrl, title, companyName))
        {
            _logger.LogInformation(
                "ATS blacklist skip title={Title} company={Company} url={Url}",
                title, companyName, sourceUrl);
            return UpsertOutcome.SkippedBlacklist;
        }

        var description = ExtractDescription(document);
        var locationLabel = FirstNonEmpty(
            ExtractLocation(document, description),
            source.DefaultLocationLabel);
        // Salary / hours are optional — leave empty when absent; do not reject the listing.
        var postal = ExtractPostal(description) ?? ExtractPostal(locationLabel);
        var locationKey = FirstNonEmpty(postal, locationLabel, companyName);

        if (!AtsListingValidation.TryValidateForReview(
                title, companyName, locationLabel, description, out var rejectReason))
        {
            _logger.LogInformation(
                "ATS invalid skip reason={Reason} title={Title} url={Url}",
                rejectReason, title, sourceUrl);
            return UpsertOutcome.SkippedInvalid;
        }

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
            _logger.LogInformation(
                "ATS INSERT hash={Hash} title={Title} company={Company} url={Url} score={Score}",
                hash[..12], title, companyName, sourceUrl, completeness);
            return UpsertOutcome.Inserted;
        }

        _logger.LogInformation(
            "ATS DEDUP hash hit hash={Hash} title={Title} existingId={Id} — refreshing row",
            hash[..12], title, existing.Id);

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

        return UpsertOutcome.Updated;
    }

    private static void ApplyOutcome(
        AtsScrapeRunReport report,
        AtsScrapeSourceReport src,
        UpsertOutcome outcome,
        string url)
    {
        switch (outcome)
        {
            case UpsertOutcome.Inserted:
                report.Inserted++;
                report.Upserted++;
                src.Inserted++;
                src.Upserted++;
                Line(report, src, $"INSERT OK {url}");
                break;
            case UpsertOutcome.Updated:
                report.Updated++;
                report.Upserted++;
                report.SkippedDuplicateHash++;
                src.Updated++;
                src.Upserted++;
                src.SkippedDuplicateHash++;
                Line(report, src, $"DEDUP refresh (hash exists) {url}");
                break;
            case UpsertOutcome.DuplicateUnchanged:
                report.SkippedDuplicateHash++;
                src.SkippedDuplicateHash++;
                Line(report, src, $"DEDUP skip unchanged {url}");
                break;
            case UpsertOutcome.SkippedBlacklist:
                report.SkippedBlacklist++;
                src.SkippedBlacklist++;
                Line(report, src, $"SKIP blacklist content {url}");
                break;
            case UpsertOutcome.SkippedParse:
                report.SkippedParse++;
                src.SkippedParse++;
                Line(report, src, $"SKIP parse (no usable title) {url}");
                break;
            case UpsertOutcome.SkippedInvalid:
                report.SkippedInvalid++;
                src.SkippedInvalid++;
                Line(report, src, $"SKIP invalid (missing required fields) {url}");
                break;
        }
    }

    internal static IReadOnlyList<string> ExtractDetailUrls(string html, string listUrl, string allowedDomain)
        => ExtractDetailUrlsWithStats(html, listUrl, allowedDomain).Urls;

    /// <summary>
    /// Pagination (?page=2, rel=next, "Volgende") and career hub URLs (werken-bij / vacatures index).
    /// </summary>
    internal static IReadOnlyList<string> ExtractPaginationAndHubUrls(
        string html,
        string listUrl,
        string allowedDomain)
    {
        var parser = new HtmlParser();
        using var document = parser.ParseDocument(html);
        if (!Uri.TryCreate(listUrl, UriKind.Absolute, out var baseUri))
        {
            return [];
        }

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var anchor in document.QuerySelectorAll(
                     "a[href], a[rel='next'], nav.pagination a[href], .pagination a[href], .pager a[href]"))
        {
            if (!TryResolveVacancyHref(anchor, baseUri, listUrl, allowedDomain, out var normalized, out var absolute))
            {
                continue;
            }

            var text = (anchor.TextContent ?? string.Empty).Trim();
            var rel = anchor.GetAttribute("rel") ?? string.Empty;
            var isNextRel = rel.Contains("next", StringComparison.OrdinalIgnoreCase);
            var isPagination = isNextRel
                               || PaginationPathHint.IsMatch(absolute.AbsoluteUri)
                               || PaginationTextHint.IsMatch(text);
            var isHub = ListHubPathHint.IsMatch(absolute.AbsolutePath.TrimEnd('/') + "/")
                        || ListHubPathHint.IsMatch(absolute.AbsolutePath);
            // Sibling career subdomain landing pages (werkenbij.*, careers.*).
            var host = absolute.Host.ToLowerInvariant();
            var isCareerSubdomain = host.StartsWith("werkenbij.", StringComparison.Ordinal)
                                    || host.StartsWith("werken-bij.", StringComparison.Ordinal)
                                    || host.StartsWith("careers.", StringComparison.Ordinal)
                                    || host.StartsWith("jobs.", StringComparison.Ordinal);

            if (isPagination || isHub || (isCareerSubdomain && VacancyPathHint.IsMatch(absolute.AbsoluteUri)))
            {
                found.Add(normalized);
            }
        }

        return found.OrderBy(u => u, StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal static bool TryBuildNextPageUrl(string listUrl, out string nextUrl)
    {
        nextUrl = string.Empty;
        if (!Uri.TryCreate(listUrl, UriKind.Absolute, out var current))
        {
            return false;
        }

        var query = current.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var key in new[] { "page", "p", "pagina", "pg", "paged" })
            {
                var match = Regex.Match(
                    query,
                    $@"(?:^|&){key}=(\d+)(?:&|$)",
                    RegexOptions.IgnoreCase);
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out var page)
                    || page < 1 || page >= 500)
                {
                    continue;
                }

                var nextQuery = Regex.Replace(
                    query,
                    $@"(^|&){key}=\d+",
                    m => $"{m.Groups[1].Value}{key}={page + 1}",
                    RegexOptions.IgnoreCase);
                var builder = new UriBuilder(current) { Query = nextQuery };
                nextUrl = NormalizeUrlKey(builder.Uri.GetLeftPart(UriPartial.Query));
                return true;
            }

            return false;
        }

        // Bare list URL → try ?page=2 once when the caller saw vacancy links.
        if (VacancyPathHint.IsMatch(current.AbsolutePath))
        {
            var builder = new UriBuilder(current) { Query = "page=2" };
            nextUrl = NormalizeUrlKey(builder.Uri.GetLeftPart(UriPartial.Query));
            return true;
        }

        return false;
    }

    private static string NormalizeUrlKey(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url.Trim();
        }

        return uri.GetLeftPart(UriPartial.Query).TrimEnd('/');
    }

    internal static (IReadOnlyList<string> Urls, int RawAnchorCount, string Strategy) ExtractDetailUrlsWithStats(
        string html,
        string listUrl,
        string allowedDomain)
    {
        var parser = new HtmlParser();
        using var document = parser.ParseDocument(html);
        if (!Uri.TryCreate(listUrl, UriKind.Absolute, out var baseUri))
        {
            return ([], 0, "none");
        }

        var allAnchors = document.QuerySelectorAll("a[href]");
        var raw = allAnchors.Length;

        // Tier 1: structural CSS hooks (.job-item, vacancy cards, href*=vacature, …).
        var structural = CollectUrls(
            document.QuerySelectorAll(string.Join(", ", StructuralVacancySelectors)),
            baseUri,
            listUrl,
            allowedDomain,
            requireVacancyHint: false);

        if (structural.Count > 0)
        {
            return (structural.OrderBy(u => u, StringComparer.OrdinalIgnoreCase).ToList(), raw, "structural");
        }

        // Tier 2: all same-domain anchors whose path/text/url match vacancy keywords.
        var keyword = CollectUrls(allAnchors, baseUri, listUrl, allowedDomain, requireVacancyHint: true);
        if (keyword.Count > 0)
        {
            return (keyword.OrderBy(u => u, StringComparer.OrdinalIgnoreCase).ToList(), raw, "keyword-fallback");
        }

        // Tier 3: broad same-domain harvest — any non-noise link under a career-ish parent path,
        // or leaf paths that look like content pages (depth >= 2) when the list URL itself
        // already sits on a jobs/vacatures section.
        var listLooksCareer = VacancyPathHint.IsMatch(listUrl);
        var broad = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var anchor in allAnchors)
        {
            if (!TryResolveVacancyHref(anchor, baseUri, listUrl, allowedDomain, out var normalized, out var absolute))
            {
                continue;
            }

            if (NonVacancyPathNoise.IsMatch(absolute.AbsolutePath))
            {
                continue;
            }

            var pathDepth = absolute.AbsolutePath.Count(c => c == '/');
            if (listLooksCareer || pathDepth >= 2)
            {
                broad.Add(normalized);
            }
        }

        if (broad.Count > 0)
        {
            return (broad.OrderBy(u => u, StringComparer.OrdinalIgnoreCase).ToList(), raw, "broad-fallback");
        }

        return ([], raw, "empty");
    }

    private static HashSet<string> CollectUrls(
        IHtmlCollection<IElement> anchors,
        Uri baseUri,
        string listUrl,
        string allowedDomain,
        bool requireVacancyHint)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var anchor in anchors)
        {
            if (!TryResolveVacancyHref(anchor, baseUri, listUrl, allowedDomain, out var normalized, out var absolute))
            {
                continue;
            }

            if (requireVacancyHint)
            {
                var text = anchor.TextContent ?? string.Empty;
                var looksLikeVacancy = VacancyPathHint.IsMatch(absolute.AbsolutePath)
                                       || VacancyPathHint.IsMatch(text)
                                       || VacancyPathHint.IsMatch(absolute.AbsoluteUri);
                if (!looksLikeVacancy)
                {
                    continue;
                }
            }

            if (NonVacancyPathNoise.IsMatch(absolute.AbsolutePath)
                && !VacancyPathHint.IsMatch(absolute.AbsolutePath))
            {
                continue;
            }

            found.Add(normalized);
        }

        return found;
    }

    private static bool TryResolveVacancyHref(
        IElement anchor,
        Uri baseUri,
        string listUrl,
        string allowedDomain,
        out string normalized,
        out Uri absolute)
    {
        normalized = string.Empty;
        absolute = baseUri;
        var href = anchor.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(href)
            || href.StartsWith('#')
            || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Uri.TryCreate(baseUri, href, out var resolved))
        {
            return false;
        }

        absolute = resolved;
        if (absolute.Scheme is not ("http" or "https"))
        {
            return false;
        }

        if (!AtsBlacklistFilter.IsDomainAllowed(absolute.AbsoluteUri, allowedDomain)
            || AtsBlacklistFilter.IsBlockedHost(absolute.AbsoluteUri))
        {
            return false;
        }

        normalized = absolute.GetLeftPart(UriPartial.Query);
        if (string.Equals(normalized.TrimEnd('/'), listUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static async Task<(HttpStatusCode Status, string Html)> FetchHtmlAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");
        using var response = await client.SendAsync(request, cancellationToken);
        var status = response.StatusCode;
        if (status is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            throw new HttpRequestException($"HTTP {(int)status} {status}");
        }

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return (status, html);
    }

    private static int? TryParseStatus(string message)
    {
        if (message.StartsWith("HTTP ", StringComparison.Ordinal)
            && int.TryParse(message.AsSpan(5, 3), out var code))
        {
            return code;
        }

        return null;
    }

    private static void Merge(AtsScrapeRunReport target, AtsScrapeRunReport part)
    {
        target.Upserted += part.Upserted;
        target.Inserted += part.Inserted;
        target.Updated += part.Updated;
        target.SkippedDuplicateHash += part.SkippedDuplicateHash;
        target.SkippedBlacklist += part.SkippedBlacklist;
        target.SkippedParse += part.SkippedParse;
        target.SkippedInvalid += part.SkippedInvalid;
        target.HttpErrors += part.HttpErrors;
        target.FailedSources += part.FailedSources;
        foreach (var s in part.Sources)
        {
            target.Sources.Add(s);
        }

        foreach (var line in part.Lines)
        {
            target.Lines.Add(line);
        }
    }

    private static void Line(AtsScrapeRunReport report, string message)
    {
        report.Lines.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
    }

    private static void Line(AtsScrapeRunReport report, AtsScrapeSourceReport src, string message)
    {
        var line = $"[{DateTime.UtcNow:HH:mm:ss}] [{src.Domain}] {message}";
        report.Lines.Add(line);
        src.Lines.Add(line);
    }

    private static string ExtractDescription(IDocument document)
    {
        var meta = MetaContent(document, "og:description")
                   ?? MetaContent(document, "description");
        var main = document.QuerySelector("[itemprop=description]")
                   ?? document.QuerySelector(".vacancy-description, .job-description, .vacature-tekst, .content-vacature")
                   ?? document.QuerySelector("main")
                   ?? document.QuerySelector("article")
                   ?? document.QuerySelector(".content, .page-content, #content")
                   ?? document.Body;
        var bodyText = CleanText(main?.TextContent);
        // Prefer substantial body text; otherwise accept shorter meta / body so incomplete pages still enter review.
        if (!string.IsNullOrWhiteSpace(bodyText) && bodyText.Length >= 40)
        {
            return bodyText;
        }

        return CleanText(meta) ?? bodyText ?? string.Empty;
    }

    private static string? ExtractLocation(IDocument document, string description)
    {
        var item = document.QuerySelector(
            "[itemprop=jobLocation], [itemprop=addressLocality], [itemprop=address], " +
            ".location, .vacancy-location, .job-location, .vacature-locatie, " +
            ".werkplek, .standplaats, address");
        var fromDom = CleanText(item?.TextContent);
        if (!string.IsNullOrWhiteSpace(fromDom) && fromDom.Length <= 200)
        {
            return fromDom;
        }

        return ExtractPostal(description);
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
