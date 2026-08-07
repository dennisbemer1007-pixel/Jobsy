using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IPartnerAffiliateService
{
    Task<PartnerAffiliateProfile> EnsureProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateProfile?> ResolveByTrackingCodeAsync(
        string? trackingCode,
        CancellationToken cancellationToken = default);

    Task<bool> ApplyReferralAsync(
        Company company,
        string? trackingCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// When a referred company spends tokens after receiving a welcome-token ledger credit,
    /// marks the referral as rewarded (once) and grants 0.5 token to the partner's company wallet.
    /// Safe to call on every spend; no-ops when not applicable.
    /// </summary>
    Task<bool> TryRewardOnWelcomeTokenSpendAsync(
        Guid referredCompanyId,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateMeDto?> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartnerAffiliateReferralRowDto>> GetReferralsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateToolkitDto?> GetToolkitAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record PartnerAffiliateMeDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string TrackingCode,
    decimal ReferralTokensEarned,
    int ReferredCompanyCount,
    int PendingReferralCount,
    int RewardedReferralCount,
    IReadOnlyList<PartnerAffiliateReferralRowDto> Referrals);

public sealed record PartnerAffiliateReferralRowDto(
    Guid CompanyId,
    string CompanyName,
    string Status,
    string StatusLabel,
    DateTime? ReferredAtUtc,
    DateTime? RewardedAtUtc,
    bool WelcomeTokenAvailable);

public sealed record PartnerAffiliateToolkitDto(
    string TrackingCode,
    string PartnerPageUrl,
    string RegisterUrl,
    string FlyerUrl);
