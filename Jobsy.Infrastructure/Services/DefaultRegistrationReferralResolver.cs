using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Fallback referral resolver until salesmanager 03 (SalesAttributionResolver + cookie) lands.
/// Typed code wins; otherwise the link/?ref= code. Validates SM-/AM-/BM-/IM- prefixes against DB.
/// </summary>
public sealed class DefaultRegistrationReferralResolver : IRegistrationReferralResolver
{
    private readonly JobsyDbContext _db;
    private readonly IPartnerAffiliateService _partners;

    public DefaultRegistrationReferralResolver(JobsyDbContext db, IPartnerAffiliateService partners)
    {
        _db = db;
        _partners = partners;
    }

    public async Task<RegistrationReferralResult> ResolveAsync(
        string? typedCode,
        string? linkCode,
        CancellationToken cancellationToken = default)
    {
        var typed = Normalize(typedCode);
        if (typed is not null)
        {
            return await ResolveKnownAsync(typed, RegistrationReferralSource.TypedCode, cancellationToken);
        }

        var link = Normalize(linkCode);
        if (link is not null)
        {
            return await ResolveKnownAsync(link, RegistrationReferralSource.LinkCode, cancellationToken);
        }

        return new RegistrationReferralResult(null, RegistrationReferralSource.None);
    }

    private async Task<RegistrationReferralResult> ResolveKnownAsync(
        string code,
        RegistrationReferralSource source,
        CancellationToken cancellationToken)
    {
        if (PartnerAffiliateService.IsPartnerTrackingCode(code))
        {
            var partner = await _partners.ResolveByTrackingCodeAsync(code, cancellationToken);
            return new RegistrationReferralResult(
                code,
                source,
                SalesManagerUserId: null,
                IsPartnerCode: true,
                IsKnown: partner is not null);
        }

        if (code.StartsWith("SM-", StringComparison.Ordinal))
        {
            var profile = await _db.SalesManagerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.TrackingCode != null
                         && p.TrackingCode.ToUpper() == code
                         && p.OnboardingCompletedAt != null,
                    cancellationToken);
            return new RegistrationReferralResult(
                code,
                source,
                SalesManagerUserId: profile?.UserId,
                IsPartnerCode: false,
                IsKnown: profile is not null);
        }

        if (code.StartsWith("AM-", StringComparison.Ordinal))
        {
            var amb = await _db.AmbassadeurProfiles.AsNoTracking()
                .AnyAsync(
                    p => p.TrackingCode != null
                         && p.TrackingCode.ToUpper() == code
                         && p.OnboardingCompletedAt != null,
                    cancellationToken);
            return new RegistrationReferralResult(
                code,
                source,
                SalesManagerUserId: null,
                IsPartnerCode: false,
                IsKnown: amb);
        }

        // Unknown prefix — still return the code so UI can show inline "unknown" without blocking.
        return new RegistrationReferralResult(code, source, IsKnown: false);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
