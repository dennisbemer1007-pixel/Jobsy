using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
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
        return MapSettings(settings);
    }

    public async Task<FlexCommercialSettingsDto> UpdateAsync(
        FlexCommercialSettingsUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (update.MarginPerHourEuro is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(update.MarginPerHourEuro), "Flex-marge moet tussen € 0 en € 100 liggen.");
        }

        if (update.DeepAnalysisPriceEuro is < 0 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(update.DeepAnalysisPriceEuro), "Diepte-analyse prijs moet tussen € 0 en € 500 liggen.");
        }

        if (update.AgencyAnnualPriceEuro is < 0 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(update.AgencyAnnualPriceEuro), "Uitzend-jaarabonnement moet tussen € 0 en € 1.000.000 liggen.");
        }

        if (update.ContactUnlockCostTokens is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(update.ContactUnlockCostTokens), "ContactUnlock moet tussen 0,1 en 100 tokens liggen.");
        }

        if (string.IsNullOrWhiteSpace(update.BackofficePartnerName))
        {
            throw new ArgumentException("Backoffice-partner is verplicht.");
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        settings.MarginPerHourEuro = Math.Round(update.MarginPerHourEuro, 2, MidpointRounding.AwayFromZero);
        settings.BackofficePartnerName = update.BackofficePartnerName.Trim();
        settings.DeepAnalysisPriceEuro = Math.Round(update.DeepAnalysisPriceEuro, 2, MidpointRounding.AwayFromZero);
        settings.AgencyAnnualPriceEuro = Math.Round(update.AgencyAnnualPriceEuro, 2, MidpointRounding.AwayFromZero);
        settings.ContactUnlockCostTokens = Math.Round(update.ContactUnlockCostTokens, 2, MidpointRounding.AwayFromZero);
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await SyncContactUnlockSpendCostAsync(settings.ContactUnlockCostTokens, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return MapSettings(settings);
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
        return row is null ? null : MapSubscription(row);
    }

    public async Task<AgencySubscriptionDto> ActivateAgencySubscriptionAsync(
        Guid companyId,
        DateTime? startsAtUtc = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        _ = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var commercial = await EnsureSettingsAsync(cancellationToken);
        var start = startsAtUtc ?? DateTime.UtcNow;
        var row = new AgencyAnnualSubscription
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            StartsAtUtc = start,
            EndsAtUtc = start.AddYears(1),
            IsActive = true,
            PriceEuro = commercial.AgencyAnnualPriceEuro,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.AgencyAnnualSubscriptions.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return MapSubscription(row);
    }

    private async Task SyncContactUnlockSpendCostAsync(decimal costTokens, CancellationToken cancellationToken)
    {
        var row = await _db.TokenSpendCosts
            .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.ContactUnlock, cancellationToken);
        if (row is null)
        {
            _db.TokenSpendCosts.Add(new TokenSpendCost
            {
                Id = Guid.NewGuid(),
                Reason = TokenSpendReason.ContactUnlock,
                CostTokens = costTokens,
                IsActive = true
            });
            return;
        }

        row.CostTokens = costTokens;
        row.IsActive = true;
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
            MarginPerHourEuro = FlexCommercialSettings.DefaultMarginPerHourEuro,
            BackofficePartnerName = FlexCommercialSettings.DefaultBackofficePartnerName,
            DeepAnalysisPriceEuro = FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
            AgencyAnnualPriceEuro = FlexCommercialSettings.DefaultAgencyAnnualPriceEuro,
            ContactUnlockCostTokens = FlexCommercialSettings.DefaultContactUnlockCostTokens,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.FlexCommercialSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static FlexCommercialSettingsDto MapSettings(FlexCommercialSettings settings) => new(
        settings.MarginPerHourEuro,
        settings.BackofficePartnerName,
        settings.DeepAnalysisPriceEuro,
        settings.AgencyAnnualPriceEuro,
        settings.ContactUnlockCostTokens,
        settings.UpdatedAtUtc);

    private static AgencySubscriptionDto MapSubscription(AgencyAnnualSubscription row) => new(
        row.Id,
        row.CompanyId,
        row.StartsAtUtc,
        row.EndsAtUtc,
        row.IsActive,
        row.PriceEuro,
        row.Note);
}
