using Jobsy.Core.Entities;

namespace Jobsy.Core.Sales;

public interface ISalesPayoutProfileService
{
    Task<SalesPortalProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<SalesPortalProfileDto> UpdateCompanyAsync(
        Guid userId,
        SalesCompanyUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<SalesPortalProfileDto> SetVatTreatmentAsync(
        Guid userId,
        SalesVatUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<SalesIbanChangeBeginResult> BeginIbanChangeAsync(
        Guid userId,
        SalesIbanChangeRequest request,
        string? authMethod,
        CancellationToken cancellationToken = default);

    Task<SalesPortalProfileDto> ConfirmIbanChangeAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);

    Task<SalesPortalProfileDto> ConfirmIbanChangeByEmailTokenAsync(
        string plaintextToken,
        Guid? expectedUserId = null,
        CancellationToken cancellationToken = default);

    Task<SalesPortalProfileDto> GiveConsentAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SalesPortalProfileDto> RevokeConsentAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SalesPortalProfileDto> SetEmailPrefsAsync(
        Guid userId,
        SalesEmailPrefs prefs,
        CancellationToken cancellationToken = default);

    Task<byte[]> RenderAgreementPdfAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<byte[]> RenderConsentPdfAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy profile PUT path: updates company (+ optional first IBAN) via this service.
    /// IBAN changes when an account already exists are ignored and reported in <see cref="SalesPortalProfileDto.Warnings"/>.
    /// </summary>
    Task<SalesPortalProfileDto> UpdateLegacyProfileAsync(
        Guid userId,
        SalesCompanyUpdateRequest company,
        string? iban,
        string? holderName,
        CancellationToken cancellationToken = default);
}

public sealed record SalesCompanyUpdateRequest(
    string CompanyName,
    string KvkNumber,
    string? VatNumber,
    string Address,
    string PostalCode,
    string City,
    string? Country = "NL");

public sealed record SalesVatUpdateRequest(
    SalesManagerVatTreatment Treatment,
    string? VatNumber,
    bool KorConfirmed);

public sealed record SalesIbanChangeRequest(
    string Iban,
    string HolderName);

public sealed record SalesIbanChangeBeginResult(
    string Method,
    bool Applied,
    SalesPortalProfileDto? Profile,
    string? Message);

public sealed class SalesPortalProfileDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = "";
    public string FullName { get; init; } = "";
    public string? CompanyName { get; init; }
    public string? KvkNumber { get; init; }
    public string? VatNumber { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string VatTreatment { get; init; } = nameof(SalesManagerVatTreatment.Standard21);
    public DateTime? VatTreatmentChangedAtUtc { get; init; }
    public string? MaskedIban { get; init; }
    public string? PayoutAccountHolderName { get; init; }
    public DateTime? IbanChangedAtUtc { get; init; }
    public DateTime? IbanPayoutHoldUntilUtc { get; init; }
    public string? TrackingCode { get; init; }
    public DateTime? AgreementSignedAt { get; init; }
    public string? AgreementVersion { get; init; }
    public DateTime? OnboardingCompletedAt { get; init; }
    public bool IsOnboardingComplete { get; init; }
    public bool CanRecruitSalesManagers { get; init; } = true;
    public Guid? ReferredBySalesManagerUserId { get; init; }
    public bool HasSelfBillingConsent { get; init; }
    public DateTime? SelfBillingConsentAtUtc { get; init; }
    public string? SelfBillingConsentVersion { get; init; }
    public SalesEmailPrefs EmailPrefs { get; init; } = SalesEmailPrefs.Default;
    public bool MfaEnabled { get; init; }
    public int IbanChangeHoldDays { get; init; } = 3;
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
