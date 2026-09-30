using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Physical letter with an 8-character verification code (D3 / 06.4).</summary>
public class CompanyVerificationLetter
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string KvkNumber { get; set; } = string.Empty;

    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }

    /// <summary>HMAC hash of the 8-character code — never plain text.</summary>
    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public int FailedAttempts { get; set; }

    public int ResendCount { get; set; }

    public Guid? ResendOfLetterId { get; set; }
    public CompanyVerificationLetter? ResendOfLetter { get; set; }

    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "NL";

    public LetterProviderKind Provider { get; set; }
    public string? ProviderLetterId { get; set; }

    public CompanyVerificationLetterStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? BlockedAtUtc { get; set; }
}
