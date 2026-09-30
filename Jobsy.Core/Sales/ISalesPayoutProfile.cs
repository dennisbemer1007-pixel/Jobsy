namespace Jobsy.Core.Sales;

/// <summary>
/// Role-agnostic payout profile (SalesManager or parked Ambassadeur).
/// </summary>
public interface ISalesPayoutProfile
{
    Guid UserId { get; }
    string? CompanyName { get; set; }
    string? KvkNumber { get; set; }
    string? VatNumber { get; set; }
    string? Address { get; set; }
    string? PostalCode { get; set; }
    string? City { get; set; }
    string? Country { get; set; }
    string? Iban { get; set; }
    Jobsy.Core.Entities.SalesManagerVatTreatment VatTreatment { get; set; }
    string? PayoutAccountHolderName { get; set; }
    DateTime? IbanChangedAtUtc { get; set; }
    DateTime? IbanPayoutHoldUntilUtc { get; set; }
    string? EmailPrefsJson { get; set; }
    string? TrackingCode { get; set; }
    string? AgreementVersion { get; set; }
    bool IsOnboardingComplete { get; }
}
