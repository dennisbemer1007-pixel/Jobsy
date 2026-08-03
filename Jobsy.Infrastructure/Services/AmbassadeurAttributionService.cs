using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class AmbassadeurAttributionService : IAmbassadeurAttributionService
{
    private readonly JobsyDbContext _db;
    private readonly IAmbassadeurSettingsService _settings;
    private readonly ILogger<AmbassadeurAttributionService> _logger;

    public AmbassadeurAttributionService(
        JobsyDbContext db,
        IAmbassadeurSettingsService settings,
        ILogger<AmbassadeurAttributionService> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Guid?> ResolveAmbassadeurUserIdAsync(
        string? trackingCode,
        CancellationToken cancellationToken = default)
    {
        var profile = await ResolveProfileAsync(trackingCode, cancellationToken);
        return profile?.UserId;
    }

    public async Task<bool> TryAttributeCandidateAsync(
        Guid candidateUserId,
        string? trackingCode,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == candidateUserId, cancellationToken);
        if (user is null || user.Role != UserRole.Candidate)
        {
            return false;
        }

        if (user.ReferredByAmbassadeurUserId is not null)
        {
            return false;
        }

        var profile = await ResolveProfileAsync(trackingCode, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        user.ReferredByAmbassadeurUserId = profile.UserId;
        user.ReferredByAmbassadeurTrackingCode = profile.TrackingCode;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Candidate {CandidateId} attributed to Ambassadeur {AmbassadeurId} via {Code}",
            candidateUserId, profile.UserId, profile.TrackingCode);
        return true;
    }

    public async Task<bool> TryAttributeCompanyAsync(
        Guid companyId,
        string? trackingCode,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return false;
        }

        if (company.ReferredByAmbassadeurUserId is not null)
        {
            return false;
        }

        var profile = await ResolveProfileAsync(trackingCode, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        var settings = await _settings.GetAsync(cancellationToken);
        var candidateCount = await _db.Users.AsNoTracking()
            .CountAsync(u => u.ReferredByAmbassadeurUserId == profile.UserId, cancellationToken);
        var percentage = AmbassadeurCommissionRules.ResolveCurrentPercentage(
            candidateCount,
            profile.BaseCommissionPercentage,
            settings.CandidateThreshold,
            settings.PercentPerThreshold,
            settings.MaxCommissionPercentage,
            profile.CommissionPercentageOverride);

        company.ReferredByAmbassadeurUserId = profile.UserId;
        company.FirstYearStartedAt ??= DateTime.UtcNow;
        company.PendingStartHighlightBonus = true;
        company.CommissionAmbassadeurRateSnapshot = AmbassadeurCommissionRules.PercentageToRate(percentage);
        if (company.CommissionTermsSnapshottedAtUtc is null)
        {
            company.CommissionDurationDaysSnapshot ??= SalesCommissionRules.DefaultCommissionDurationDays;
            company.CommissionTermsSnapshottedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Company {CompanyId} attributed to Ambassadeur {AmbassadeurId} via {Code} (rate {Rate})",
            companyId, profile.UserId, profile.TrackingCode, company.CommissionAmbassadeurRateSnapshot);
        return true;
    }

    public async Task RecalculateAndPersistCurrentRateSnapshotAsync(
        Guid ambassadeurUserId,
        CancellationToken cancellationToken = default)
    {
        // Snapshots on companies stay frozen at attribution; this method is reserved for future
        // live-rate dashboards / admin recalculation hooks.
        await Task.CompletedTask;
        _ = ambassadeurUserId;
        _ = cancellationToken;
    }

    private async Task<AmbassadeurProfile?> ResolveProfileAsync(
        string? trackingCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var code = trackingCode.Trim().ToUpperInvariant();
        if (!AmbassadeurCommissionRules.IsAmbassadeurTrackingCode(code)
            && !code.StartsWith(AmbassadeurCommissionRules.TrackingCodePrefix, StringComparison.Ordinal))
        {
            // Allow demo / longer codes that still start with AM-
            if (!code.StartsWith(AmbassadeurCommissionRules.TrackingCodePrefix, StringComparison.Ordinal))
            {
                return null;
            }
        }

        return await _db.AmbassadeurProfiles
            .FirstOrDefaultAsync(
                p => p.TrackingCode != null
                     && p.TrackingCode.ToUpper() == code
                     && p.OnboardingCompletedAt != null,
                cancellationToken);
    }
}
