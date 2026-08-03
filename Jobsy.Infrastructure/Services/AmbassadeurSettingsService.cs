using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AmbassadeurSettingsService : IAmbassadeurSettingsService
{
    private static readonly Guid SettingsId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    private readonly JobsyDbContext _db;

    public AmbassadeurSettingsService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<AmbassadeurSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await EnsureAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<AmbassadeurSettingsDto> UpdateAsync(
        AmbassadeurSettingsUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CandidateThreshold < 1)
        {
            throw new ArgumentException("Drempelwaarde moet minimaal 1 zijn.");
        }

        if (request.PercentPerThreshold < 0 || request.PercentPerThreshold > 100)
        {
            throw new ArgumentException("Percentage per drempel moet tussen 0 en 100 liggen.");
        }

        if (request.MaxCommissionPercentage < AmbassadeurCommissionRules.DefaultBaseCommissionPercentage
            || request.MaxCommissionPercentage > 100)
        {
            throw new ArgumentException(
                $"Maximum commissiepercentage moet tussen {AmbassadeurCommissionRules.DefaultBaseCommissionPercentage} en 100 liggen.");
        }

        var settings = await EnsureAsync(cancellationToken);
        settings.CandidateThreshold = request.CandidateThreshold;
        settings.PercentPerThreshold = decimal.Round(request.PercentPerThreshold, 2, MidpointRounding.AwayFromZero);
        settings.MaxCommissionPercentage = decimal.Round(request.MaxCommissionPercentage, 2, MidpointRounding.AwayFromZero);
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    public async Task SetCommissionOverrideAsync(
        Guid ambassadeurUserId,
        decimal? percentageOverride,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.AmbassadeurProfiles
            .FirstOrDefaultAsync(p => p.UserId == ambassadeurUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Ambassadeur-profiel niet gevonden.");

        var settings = await EnsureAsync(cancellationToken);
        if (percentageOverride is decimal value)
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentException("Commissie-override moet tussen 0 en 100 liggen.");
            }

            profile.CommissionPercentageOverride = AmbassadeurCommissionRules.ClampPercentage(
                value, settings.MaxCommissionPercentage);
        }
        else
        {
            profile.CommissionPercentageOverride = null;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AmbassadeurSettings> EnsureAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.AmbassadeurSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new AmbassadeurSettings
        {
            Id = SettingsId,
            CandidateThreshold = AmbassadeurCommissionRules.DefaultCandidateThreshold,
            PercentPerThreshold = AmbassadeurCommissionRules.DefaultPercentPerThreshold,
            MaxCommissionPercentage = AmbassadeurCommissionRules.DefaultMaxCommissionPercentage,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.AmbassadeurSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static AmbassadeurSettingsDto Map(AmbassadeurSettings s) =>
        new(s.CandidateThreshold, s.PercentPerThreshold, s.MaxCommissionPercentage, s.UpdatedAtUtc);
}
