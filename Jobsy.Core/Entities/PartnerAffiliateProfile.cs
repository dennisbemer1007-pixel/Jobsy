using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Lightweight affiliate profile for Bedrijfsmanagers and Intermediairs.
/// Tracking codes are issued automatically when the role is activated.
/// </summary>
public class PartnerAffiliateProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TrackingCode { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static string PrefixForRole(UserRole role) => role switch
    {
        UserRole.EnterpriseManager => "BM-",
        UserRole.Intermediary => "IM-",
        _ => throw new InvalidOperationException("Alleen Bedrijfsmanager en Intermediair krijgen een partnercode.")
    };
}
