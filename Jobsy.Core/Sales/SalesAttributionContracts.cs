using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

public enum SalesResolvedCodeKind
{
    None = 0,
    SalesManager = 1,
    Partner = 2
}

public enum SalesSelfReferralRule
{
    None = 0,
    SameUser = 1,
    SameEmail = 2,
    SameKvk = 3,
    SameEmailDomain = 4
}

/// <summary>Winning code at registration: typed wins over cookie (D2).</summary>
public sealed record SalesAttributionResolution(
    SalesResolvedCodeKind Kind,
    string? Code,
    Guid? BeneficiaryUserId,
    SalesAttributionSource? Source,
    SalesSelfReferralRule BlockedBy = SalesSelfReferralRule.None)
{
    public static SalesAttributionResolution None { get; } = new(
        SalesResolvedCodeKind.None, null, null, null);

    public bool IsAttributed => Kind != SalesResolvedCodeKind.None && BeneficiaryUserId is not null;
    public bool IsBlocked => BlockedBy != SalesSelfReferralRule.None;
}

public interface ISalesAttributionResolver
{
    /// <summary>
    /// Resolves typed code → cookie → none. Partner codes keep the partner path;
    /// AM- codes are ignored while ambassadors are parked.
    /// </summary>
    Task<SalesAttributionResolution> ResolveAtRegistrationAsync(
        string? typedCode,
        string? cookieCode,
        string registeringEmail,
        string? registeringKvkNumber,
        Guid? registeringUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active salesmanager or partner code suitable for setting the referral cookie / counting clicks.
    /// Returns null for unknown, inactive, or parked AM- codes.
    /// </summary>
    Task<SalesActiveReferral?> ResolveActiveReferralAsync(
        string? code,
        CancellationToken cancellationToken = default);
}

public sealed record SalesActiveReferral(
    string Code,
    Guid BeneficiaryUserId,
    SalesResolvedCodeKind Kind);

public interface ISalesLinkClickService
{
    /// <summary>
    /// Upserts a daily click counter. Never stores IP, UA, referrer or cookie id.
    /// Caller decides same-day / bot skips before calling.
    /// </summary>
    Task RecordClickAsync(
        Guid beneficiaryUserId,
        SalesLinkChannel channel,
        DateOnly? localDate = null,
        CancellationToken cancellationToken = default);
}

public interface ISalesAttributionAdminService
{
    Task<SalesAttributionChange> ReassignAsync(
        Guid companyId,
        Guid? toBeneficiaryUserId,
        string reason,
        Guid changedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAttributionHistoryItem>> GetHistoryAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

public sealed record SalesAttributionHistoryItem(
    Guid Id,
    Guid CompanyId,
    Guid? FromUserId,
    Guid? ToUserId,
    string Source,
    string Reason,
    Guid ChangedByUserId,
    DateTime ChangedAtUtc);

public interface ISalesFunnelReadService
{
    Task<SalesFunnelSnapshot> GetAsync(
        Guid beneficiaryUserId,
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken = default);
}

public sealed record SalesFunnelSnapshot(
    int Visits,
    int Registered,
    int FirstPurchase,
    int ActiveNow);
