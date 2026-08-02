namespace Jobsy.Core.Entities;

/// <summary>
/// Binds an external identity provider subject (e.g. Microsoft Entra OID) to a Lobsy user.
/// Matching prefers Provider+Subject over e-mail so IdP e-mail changes do not orphan accounts.
/// </summary>
public class UserExternalLogin
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Provider key, e.g. <c>entra</c> or <c>google</c>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Stable subject from the IdP (Entra OID / OIDC <c>sub</c>).</summary>
    public string ProviderSubject { get; set; } = string.Empty;

    /// <summary>Verified e-mail at the moment of first link (audit only).</summary>
    public string? EmailAtLink { get; set; }

    public DateTime LinkedAtUtc { get; set; }
}
