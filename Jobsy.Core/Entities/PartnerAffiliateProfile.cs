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

    public string? CompanyName { get; set; }
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "NL";
    public string? Iban { get; set; }

    public string TrackingCode { get; set; } = string.Empty;

    /// <summary>When the partner mediation agreement was accepted (server-stamped).</summary>
    public DateTime? AgreementSignedAt { get; set; }

    /// <summary>Server-controlled agreement version; client-supplied versions are ignored.</summary>
    public string? AgreementVersion { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public bool IsOnboardingComplete =>
        !string.IsNullOrWhiteSpace(TrackingCode)
        && AgreementSignedAt.HasValue
        && !string.IsNullOrWhiteSpace(AgreementVersion);

    public static string PrefixForRole(UserRole role) => role switch
    {
        UserRole.EnterpriseManager => "BM-",
        UserRole.Intermediary => "IM-",
        _ => throw new InvalidOperationException("Alleen Bedrijfsmanager en Intermediair krijgen een partnercode.")
    };
}
