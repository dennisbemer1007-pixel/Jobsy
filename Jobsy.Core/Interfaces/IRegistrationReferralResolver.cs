namespace Jobsy.Core.Interfaces;

/// <summary>
/// Resolves sales/partner referral attribution for employer self-registration.
/// Typed code wins over link/cookie code. Fallback until salesmanager 03 lands.
/// </summary>
public interface IRegistrationReferralResolver
{
    Task<RegistrationReferralResult> ResolveAsync(
        string? typedCode,
        string? linkCode,
        CancellationToken cancellationToken = default);
}

public enum RegistrationReferralSource
{
    None = 0,
    TypedCode = 1,
    LinkCode = 2,
    Cookie = 3
}

public sealed record RegistrationReferralResult(
    string? Code,
    RegistrationReferralSource Source,
    Guid? SalesManagerUserId = null,
    bool IsPartnerCode = false,
    bool IsKnown = false);
