using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class AtsVacancyModerationService : IAtsVacancyModerationService
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<AtsVacancyModerationService> _logger;

    public AtsVacancyModerationService(
        JobsyDbContext db,
        ILogger<AtsVacancyModerationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AtsScrapedListing>> ListAsync(
        AtsListingStatus? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var q = _db.AtsScrapedListings.AsNoTracking()
            .Include(l => l.Source)
            .Where(l =>
                !l.Title.Contains("(demo)")
                && !l.SourceUrl.Contains("/demo-")
                && (l.TagsJson == null || !l.TagsJson.Contains("\"demo\"")))
            .AsQueryable();

        if (status is not null)
        {
            q = q.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(l =>
                l.Title.ToLower().Contains(term)
                || l.CompanyName.ToLower().Contains(term)
                || (l.LocationLabel != null && l.LocationLabel.ToLower().Contains(term))
                || l.SourceUrl.ToLower().Contains(term));
        }

        return await q
            .OrderByDescending(l => l.ScrapedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public Task<AtsScrapedListing?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.AtsScrapedListings
            .Include(l => l.Source)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<AtsScrapedListing?> UpdateFieldsAsync(
        Guid id,
        string title,
        string companyName,
        string? locationLabel,
        string description,
        string? salaryText,
        decimal? hourlyWage,
        string? hoursText,
        CancellationToken cancellationToken = default)
    {
        var listing = await _db.AtsScrapedListings
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        if (AtsBlacklistFilter.IsBlocked(listing.SourceUrl, title, companyName))
        {
            throw new InvalidOperationException(
                "Titel of bedrijfsnaam bevat geblokkeerde uitzend-/recruitment-termen.");
        }

        listing.Title = TruncateRequired(title, 256);
        listing.CompanyName = TruncateRequired(companyName, 256);
        listing.LocationLabel = Truncate(locationLabel, 256);
        listing.Description = TruncateRequired(description, 20_000);
        if (!AtsListingValidation.TryValidateForReview(
                listing.Title, listing.CompanyName, listing.LocationLabel, listing.Description, out var reason))
        {
            throw new InvalidOperationException(reason ?? "Listing is onvolledig.");
        }

        listing.SalaryText = Truncate(salaryText, 512);
        listing.HourlyWage = hourlyWage;
        listing.HoursText = Truncate(hoursText, 256);
        listing.DedupHash = AtsDedupeHash.Compute(
            listing.CompanyName,
            listing.Title,
            listing.PostalCode ?? listing.LocationLabel);
        listing.CompletenessScore = AtsCompletenessScore.Compute(
            listing.Title,
            listing.CompanyName,
            listing.LocationLabel,
            listing.Description,
            listing.SalaryText,
            listing.HourlyWage,
            listing.HoursText,
            listing.MinHoursPerWeek,
            listing.MaxHoursPerWeek,
            listing.TagsJson,
            listing.SourceUrl);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(
                "Opslaan mislukt — mogelijk een dubbele vacature (zelfde bedrijf+titel+locatie).", ex);
        }

        return listing;
    }

    public async Task<Vacancy?> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _db.AtsScrapedListings
            .Include(l => l.Source)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        if (listing.Status is AtsListingStatus.Rejected or AtsListingStatus.Expired)
        {
            throw new InvalidOperationException("Afgekeurde of verlopen listings kunnen niet worden goedgekeurd.");
        }

        if (AtsBlacklistFilter.IsBlocked(listing.SourceUrl, listing.Title, listing.CompanyName))
        {
            throw new InvalidOperationException("Listing is geblokkeerd door de ATS-blacklist.");
        }

        var now = DateTime.UtcNow;
        if (listing.ExpiresAtUtc is null)
        {
            listing.ExpiresAtUtc = AtsVacancyRules.DefaultExpiresAt(listing.ScrapedAtUtc);
        }

        if (listing.ExpiresAtUtc <= now)
        {
            listing.Status = AtsListingStatus.Expired;
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Listing is verlopen (TTL 30 dagen).");
        }

        Vacancy vacancy;
        if (listing.LinkedVacancyId is Guid linkedId)
        {
            vacancy = await _db.Vacancies.FirstOrDefaultAsync(v => v.Id == linkedId, cancellationToken)
                      ?? throw new InvalidOperationException("Gekoppelde vacature ontbreekt.");
            ApplyListingToVacancy(listing, vacancy, now);
        }
        else
        {
            var company = await ResolveCompanyAsync(listing, cancellationToken);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, company.Id, cancellationToken);
            var salaryTableId = await _db.CompanySalaryTables.AsNoTracking()
                .Where(t => t.IsActive && t.IsSystemWml
                            && (t.CompanyId == company.Id
                                || t.CompanyId == (company.ParentCompanyId ?? company.Id)))
                .OrderBy(t => t.Id)
                .Select(t => (Guid?)t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var adultWage = listing.HourlyWage;
            if (adultWage is null or <= 0 && salaryTableId is Guid tableId)
            {
                adultWage = await _db.CompanySalaryRates.AsNoTracking()
                    .Where(r => r.SalaryTableId == tableId && r.AgeYears >= 21)
                    .OrderBy(r => r.AgeYears)
                    .Select(r => (decimal?)r.HourlyRate)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            vacancy = new Vacancy
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                CreatedVia = VacancySource.Ats,
                CreatedAtUtc = now,
                PublishedAtUtc = now,
                Status = VacancyStatus.Active,
                ContentModerationPassed = true,
                MaxApplications = 5,
                RequiredTransport = TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Walking,
                SalaryTableId = salaryTableId,
                FlexibleTimes = listing.MinHoursPerWeek is null && listing.MaxHoursPerWeek is null,
                FlexibleScheduleSource = listing.MinHoursPerWeek is null && listing.MaxHoursPerWeek is null
                    ? "AtsEmpty"
                    : null
            };
            ApplyListingToVacancy(listing, vacancy, now);
            if (adultWage is > 0)
            {
                vacancy.HourlyWage = adultWage.Value;
            }

            _db.Vacancies.Add(vacancy);
            listing.LinkedVacancyId = vacancy.Id;
        }

        listing.Status = AtsListingStatus.Approved;
        listing.ReviewedAtUtc = now;
        listing.RejectReason = null;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "ATS listing {ListingId} approved → vacancy {VacancyId}.",
            listing.Id, vacancy.Id);
        return vacancy;
    }

    public async Task RejectAsync(Guid id, string? reason, CancellationToken cancellationToken = default)
    {
        var listing = await _db.AtsScrapedListings
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Listing niet gevonden.");

        listing.Status = AtsListingStatus.Rejected;
        listing.RejectReason = Truncate(reason, 1000);
        listing.ReviewedAtUtc = DateTime.UtcNow;

        if (listing.LinkedVacancyId is Guid vid)
        {
            var vacancy = await _db.Vacancies.FirstOrDefaultAsync(v => v.Id == vid, cancellationToken);
            if (vacancy is not null && vacancy.Status == VacancyStatus.Active)
            {
                vacancy.Status = VacancyStatus.Archived;
                vacancy.ClosedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _db.AtsScrapedListings
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (listing is null)
        {
            return;
        }

        if (listing.LinkedVacancyId is Guid vid)
        {
            var vacancy = await _db.Vacancies.FirstOrDefaultAsync(v => v.Id == vid, cancellationToken);
            if (vacancy is not null && vacancy.CreatedVia == VacancySource.Ats)
            {
                vacancy.Status = VacancyStatus.Archived;
                vacancy.ClosedAtUtc ??= DateTime.UtcNow;
            }
        }

        _db.AtsScrapedListings.Remove(listing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyListingToVacancy(AtsScrapedListing listing, Vacancy vacancy, DateTime now)
    {
        var lat = listing.Latitude ?? listing.Source?.DefaultLatitude ?? 52.0705;
        var lng = listing.Longitude ?? listing.Source?.DefaultLongitude ?? 4.3007;
        var start = DateOnly.FromDateTime(now);
        var expiry = listing.ExpiresAtUtc ?? AtsVacancyRules.DefaultExpiresAt(listing.ScrapedAtUtc);
        var end = DateOnly.FromDateTime(expiry);
        if (end < start)
        {
            end = start.AddDays(1);
        }

        var description = listing.Description;
        if (!string.IsNullOrWhiteSpace(listing.SourceUrl)
            && !description.Contains(listing.SourceUrl, StringComparison.OrdinalIgnoreCase))
        {
            description = $"{description.Trim()}\n\nBron: {listing.SourceUrl}".Trim();
            if (description.Length > 20_000)
            {
                description = description[..20_000];
            }
        }

        vacancy.Title = listing.Title;
        vacancy.Description = description;
        vacancy.Location = new GeoPoint(lat, lng);
        vacancy.StartDate = start;
        vacancy.EndDate = end;
        vacancy.Status = VacancyStatus.Active;
        vacancy.PublishedAtUtc ??= now;
        vacancy.CreatedVia = VacancySource.Ats;
        vacancy.ImageUrl = listing.ImageUrl;
        vacancy.MinHoursPerWeek = listing.MinHoursPerWeek;
        vacancy.MaxHoursPerWeek = listing.MaxHoursPerWeek;
        if (listing.HourlyWage is > 0)
        {
            vacancy.HourlyWage = listing.HourlyWage.Value;
        }
        else if (vacancy.HourlyWage <= 0)
        {
            vacancy.HourlyWage = 14.00m;
        }

        vacancy.WorkTypeLabels = string.IsNullOrWhiteSpace(listing.TagsJson)
            ? null
            : Truncate(listing.TagsJson.Trim('[', ']').Replace("\"", string.Empty), 512);
    }

    private async Task<Company> ResolveCompanyAsync(
        AtsScrapedListing listing,
        CancellationToken cancellationToken)
    {
        if (listing.Source.PreferredCompanyId is Guid preferredId)
        {
            var preferred = await _db.Companies
                .FirstOrDefaultAsync(c => c.Id == preferredId, cancellationToken);
            if (preferred is not null)
            {
                return preferred;
            }
        }

        var name = listing.CompanyName.Trim();
        var existing = await _db.Companies
            .FirstOrDefaultAsync(
                c => c.Type == CompanyType.Employer
                     && c.Name.ToLower() == name.ToLower(),
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var lat = listing.Latitude ?? listing.Source?.DefaultLatitude ?? 52.0705;
        var lng = listing.Longitude ?? listing.Source?.DefaultLongitude ?? 4.3007;
        var kvkSuffix = listing.DedupHash.Length >= 8
            ? listing.DedupHash[..8].ToUpperInvariant()
            : listing.DedupHash.ToUpperInvariant().PadRight(8, '0');
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = TruncateRequired(name, 256),
            KvkNumber = ("ATS" + kvkSuffix).Length <= 12
                ? "ATS" + kvkSuffix
                : ("ATS" + kvkSuffix)[..12],
            KvkEstablishmentId = "ats_" + (listing.DedupHash.Length >= 16
                ? listing.DedupHash[..16]
                : listing.DedupHash),
            KvkVerificationStatus = KvkVerificationStatus.Pending,
            Address = Truncate(listing.LocationLabel, 256) ?? listing.Source?.DefaultLocationLabel ?? "Nederland",
            Location = new GeoPoint(lat, lng),
            Type = CompanyType.Employer
        };
        _db.Companies.Add(company);
        return company;
    }

    private static string TruncateRequired(string value, int max)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Verplicht veld ontbreekt.");
        }

        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
