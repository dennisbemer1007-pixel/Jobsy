using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class FlexCommercialService : IFlexCommercialService
{
    public static readonly Guid SettingsSingletonId = Guid.Parse("f1e20001-0000-4000-8000-000000000001");

    private readonly JobsyDbContext _db;

    public FlexCommercialService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<FlexCommercialSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        return new FlexCommercialSettingsDto(
            settings.MarginPerHourEuro,
            settings.BackofficePartnerName,
            settings.UpdatedAtUtc);
    }

    public async Task<FlexCommercialSettingsDto> UpdateAsync(
        decimal marginPerHourEuro,
        string backofficePartnerName,
        CancellationToken cancellationToken = default)
    {
        if (marginPerHourEuro < 0 || marginPerHourEuro > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(marginPerHourEuro), "Marge moet tussen € 0 en € 100 liggen.");
        }

        if (string.IsNullOrWhiteSpace(backofficePartnerName))
        {
            throw new ArgumentException("Backoffice-partner is verplicht.");
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        settings.MarginPerHourEuro = Math.Round(marginPerHourEuro, 2, MidpointRounding.AwayFromZero);
        settings.BackofficePartnerName = backofficePartnerName.Trim();
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }

    public async Task<bool> HasActiveAgencySubscriptionAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.AgencyAnnualSubscriptions.AsNoTracking()
            .AnyAsync(
                s => s.CompanyId == companyId
                     && s.IsActive
                     && s.StartsAtUtc <= now
                     && s.EndsAtUtc > now,
                cancellationToken);
    }

    public async Task<AgencySubscriptionDto?> GetAgencySubscriptionAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var row = await _db.AgencyAnnualSubscriptions.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.IsActive && s.EndsAtUtc > now)
            .OrderByDescending(s => s.EndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<AgencySubscriptionDto> ActivateAgencySubscriptionAsync(
        Guid companyId,
        DateTime? startsAtUtc = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        _ = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var start = startsAtUtc ?? DateTime.UtcNow;
        var row = new AgencyAnnualSubscription
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            StartsAtUtc = start,
            EndsAtUtc = start.AddYears(1),
            IsActive = true,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.AgencyAnnualSubscriptions.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    private async Task<FlexCommercialSettings> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.FlexCommercialSettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new FlexCommercialSettings
        {
            Id = SettingsSingletonId,
            MarginPerHourEuro = 2.00m,
            BackofficePartnerName = "Yellowstone",
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.FlexCommercialSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static AgencySubscriptionDto Map(AgencyAnnualSubscription row) => new(
        row.Id,
        row.CompanyId,
        row.StartsAtUtc,
        row.EndsAtUtc,
        row.IsActive,
        AgencyAnnualSubscription.AnnualPriceEuro,
        row.Note);
}
