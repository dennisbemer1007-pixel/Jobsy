using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record CompanySummaryDto(
    Guid Id,
    string Name,
    string Address,
    string KvkNumber,
    decimal TokenBalance,
    int ActiveVacancies,
    Guid? ParentCompanyId = null,
    bool TokensManagedByEnterprise = false,
    bool CsvBatchImportEnabled = false,
    bool DirectContactEnabled = false,
    bool ContactPreferMail = false,
    bool ContactPreferPhone = false,
    bool ContactPreferWhatsApp = false,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? ContactWhatsApp = null,
    string? KvkEstablishmentId = null,
    string KvkVerificationStatus = nameof(Jobsy.Core.Enums.KvkVerificationStatus.Verified),
    string? PreferredPaymentMethod = null,
    bool RequireEmailVerificationForApplications = false);

public record UpdateBillingPreferenceRequest(string? PreferredPaymentMethod);

public record CompanyBillingHistoryItemDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CheckoutId,
    string? PaymentMethod,
    string PaymentMethodLabel,
    int PackSize,
    decimal AmountExVatEuro,
    decimal VatAmountEuro,
    decimal TotalAmountEuro,
    DateTime IssuedAt,
    string Status);

public record TokenBalanceDto(
    Guid CompanyId,
    string CompanyName,
    decimal Balance,
    Guid? ParentCompanyId = null,
    bool TokensManagedByEnterprise = false);

public record UpdateTokenManagementRequest(bool TokensManagedByEnterprise);

public record UpdateCsvBatchImportRequest(bool CsvBatchImportEnabled);

public record UpdateEmailVerificationPreferenceRequest(bool RequireEmailVerificationForApplications);

public record UpdateContactPreferenceRequest(
    bool DirectContactEnabled,
    bool ContactPreferMail,
    bool ContactPreferPhone,
    bool ContactPreferWhatsApp,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? ContactWhatsApp = null);

public record VacancyContactPreferenceDto(
    Guid VacancyId,
    bool OverrideContactPreference,
    bool DirectContactEnabled,
    bool ContactPreferMail,
    bool ContactPreferPhone,
    bool ContactPreferWhatsApp);

public record VacancyEmailVerificationDto(Guid VacancyId, bool RequireEmailVerification);

public record UpdateVacancyContactPreferenceRequest(
    bool OverrideContactPreference,
    bool DirectContactEnabled = false,
    bool ContactPreferMail = false,
    bool ContactPreferPhone = false,
    bool ContactPreferWhatsApp = false);

public record UpdateVacancyEmailVerificationRequest(bool RequireEmailVerification);

public record GrantTokensRequest(Guid CompanyId, decimal Amount, string Note);

public record ApplicationDto(
    Guid Id,
    Guid VacancyId,
    string VacancyTitle,
    string CompanyName,
    string CandidateName,
    string CandidateEmail,
    string PreferredTransport,
    int EstimatedTravelMinutes,
    DateTime CreatedAt,
    string Status,
    DateTime? RespondedAt = null,
    string? LocationLabel = null);

public record ApplyRequest(
    Guid VacancyId,
    string PreferredTransport,
    int EstimatedTravelMinutes,
    bool UseAuthenticator = false,
    bool AcceptedTerms = false,
    string? ConsentVersion = null,
    bool WorkPermitConfirmed = false,
    string? VerificationCode = null,
    string? Motivation = null,
    bool ConfirmLowMatchSafetyNet = false,
    string? StudentNumber = null,
    string? SchoolEmail = null,
    string? StudyProgram = null,
    string? StudyYear = null);

public record ReactToApplicationRequest(ApplicationStatus Status);

public record ApplyResultDto(
    ApplicationDto Application,
    bool ConfirmationEmailQueued,
    bool AuthenticatorStubUsed,
    bool RequiresVerification = false,
    bool VerificationCodeSent = false,
    EmployerDirectContactDto? DirectContact = null,
    bool RequiresSafetyNetConfirmation = false,
    int? MatchPercent = null,
    string? MatchBreakdownJson = null,
    string? SafetyNetMessage = null);

/// <summary>
/// Revealed only after a successful (verified) application. Never included on public vacancy payloads.
/// </summary>
public record EmployerDirectContactDto(
    bool Available,
    bool OfferMail,
    bool OfferPhone,
    bool OfferWhatsApp,
    string? Email = null,
    string? Phone = null,
    string? WhatsAppUrl = null,
    string? WhatsAppNumber = null);

public record MinimumWageRateDto(
    Guid Id,
    int AgeYears,
    decimal HourlyRate,
    string Label,
    DateOnly EffectiveFrom);

public record UpsertWageRateRequest(
    Guid? Id,
    int AgeYears,
    decimal HourlyRate,
    string Label,
    DateOnly EffectiveFrom);

public record MeAccessDto(
    string? Role,
    bool IsAdmin,
    bool IsEmployer,
    bool IsCandidate,
    IReadOnlyCollection<Guid>? AccessibleCompanyIds,
    bool AllCompanies);

public record MeProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    DateOnly? DateOfBirth,
    bool HasDateOfBirth,
    bool OpenForWork,
    CandidatePreferencesDto Preferences,
    bool AuthenticatorEnabled,
    double? HomeLatitude = null,
    double? HomeLongitude = null,
    string? ConsentVersion = null,
    bool NeedsConsentReaccept = false,
    string CurrentConsentVersion = "",
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    bool WhatsAppContactAllowed = false,
    CandidateUploadedCvInfoDto? UploadedCv = null,
    IReadOnlyList<CandidateReferenceDto>? References = null);

public record CandidateUploadedCvInfoDto(
    string FileName,
    string ContentType,
    int SizeBytes,
    DateTime UploadedAtUtc,
    DateTime? ExtractedAtUtc = null,
    IReadOnlyList<string>? FilledFields = null);

public record CandidateReferenceDto(
    Guid Id,
    string EmployerName,
    string ContactName,
    string Email,
    string Phone);

public record UpdateDateOfBirthRequest(DateOnly DateOfBirth);

public record UpdateCandidateProfileRequest(
    bool? OpenForWork,
    DateOnly? DateOfBirth,
    CandidatePreferencesDto? Preferences,
    double? HomeLatitude = null,
    double? HomeLongitude = null,
    bool ClearHomeLocation = false,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    bool? WhatsAppContactAllowed = null,
    IReadOnlyList<CandidateReferenceDto>? References = null);

public record UpdateLanguageRequest(string Language);

public record RecordClickRequest(string? AnonymousKey);

public record RecordImpressionsRequest(IReadOnlyList<Guid>? VacancyIds, string? AnonymousKey);

public record RecordSiteVisitRequest(string? AnonymousKey, string? Path);

public record ShareVacancyRequest(ShareChannel Channel);

public record LikeStatusDto(bool Liked);

public record ShareRecordedDto(Guid Id, ShareChannel Channel, DateTime CreatedAt);

public record PublishVacancyRequest(
    Guid VacancyId,
    bool Highlight = false,
    bool PushBom = false,
    bool Extend = false);

public record PushBomPreviewDto(
    int CandidateCount,
    decimal CostTokens,
    double RadiusKm,
    int MaxTravelMinutes,
    bool HasPricing,
    decimal TokenBalance,
    bool CanAfford);

public record VacancyProductActionResultDto(
    VacancyListItemDto Vacancy,
    bool PendingApproval = false,
    string? Message = null,
    int PushBomRecipientCount = 0);

/// <summary>Structured 402 body when a prepaid token top-up is required.</summary>
public record InsufficientTokensDto(
    string Code,
    string Message,
    Guid CompanyId,
    Guid VacancyId,
    string Action,
    decimal RequiredTokens,
    decimal Balance,
    decimal Deficit,
    bool Highlight = false,
    bool PushBom = false,
    bool Extend = false);

