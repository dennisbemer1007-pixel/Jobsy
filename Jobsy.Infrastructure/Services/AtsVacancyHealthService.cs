using System.Net;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Weekly URL/content health + hard 30-day TTL for ATS listings (and linked vacancies).
/// </summary>
public sealed class AtsVacancyHealthService : IAtsVacancyHealthService
{
    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AtsVacancyHealthService> _logger;

    public AtsVacancyHealthService(
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<AtsVacancyHealthService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<int> RunHealthPassAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = 0;

        // Hard TTL first (no HTTP needed).
        var pastTtl = await _db.AtsScrapedListings
            .Where(l => l.Status == AtsListingStatus.PendingReview
                        || l.Status == AtsListingStatus.Approved
                        || l.Status == AtsListingStatus.Inactive)
            .Where(l => (l.ExpiresAtUtc != null && l.ExpiresAtUtc <= now)
                        || l.ScrapedAtUtc <= now.AddDays(-AtsVacancyRules.TimeToLiveDays))
            .Take(300)
            .ToListAsync(cancellationToken);

        foreach (var listing in pastTtl)
        {
            await MarkExpiredAsync(listing, now, cancellationToken);
            expired++;
        }

        if (expired > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Weekly URL / content presence for still-active rows.
        var staleBefore = now.AddDays(-7);
        var toCheck = await _db.AtsScrapedListings
            .Where(l => l.Status == AtsListingStatus.PendingReview
                        || l.Status == AtsListingStatus.Approved)
            .Where(l => l.LastCheckedAtUtc == null || l.LastCheckedAtUtc <= staleBefore)
            .OrderBy(l => l.LastCheckedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (toCheck.Count == 0)
        {
            return expired;
        }

        var client = _httpClientFactory.CreateClient(AtsScrapeService.HttpClientName);
        var inactivated = 0;
        foreach (var listing in toCheck)
        {
            cancellationToken.ThrowIfCancellationRequested();
            listing.LastCheckedAtUtc = now;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, listing.SourceUrl);
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");
                using var response = await client.SendAsync(request, cancellationToken);
                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    listing.Status = AtsListingStatus.Inactive;
                    await ArchiveLinkedAsync(listing, now, cancellationToken);
                    inactivated++;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var titlePresent = !string.IsNullOrWhiteSpace(listing.Title)
                    && html.Contains(listing.Title, StringComparison.OrdinalIgnoreCase);
                var companyPresent = string.IsNullOrWhiteSpace(listing.CompanyName)
                    || html.Contains(listing.CompanyName, StringComparison.OrdinalIgnoreCase);
                if (!titlePresent && !companyPresent)
                {
                    listing.Status = AtsListingStatus.Inactive;
                    await ArchiveLinkedAsync(listing, now, cancellationToken);
                    inactivated++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "ATS health check failed for {Url}", listing.SourceUrl);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "ATS health pass: ttlExpired={Expired}, inactivated={Inactivated}, checked={Checked}.",
            expired, inactivated, toCheck.Count);
        return expired + inactivated;
    }

    private async Task MarkExpiredAsync(
        Core.Entities.AtsScrapedListing listing,
        DateTime now,
        CancellationToken cancellationToken)
    {
        listing.Status = AtsListingStatus.Expired;
        listing.LastCheckedAtUtc = now;
        await ArchiveLinkedAsync(listing, now, cancellationToken);
    }

    private async Task ArchiveLinkedAsync(
        Core.Entities.AtsScrapedListing listing,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (listing.LinkedVacancyId is not Guid vid)
        {
            return;
        }

        var vacancy = await _db.Vacancies.FirstOrDefaultAsync(v => v.Id == vid, cancellationToken);
        if (vacancy is null || vacancy.CreatedVia != VacancySource.Ats)
        {
            return;
        }

        if (vacancy.Status == VacancyStatus.Active)
        {
            vacancy.Status = VacancyStatus.Archived;
            vacancy.ClosedAtUtc = now;
        }
    }
}
