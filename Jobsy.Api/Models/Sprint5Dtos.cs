using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record TokenPackDto(int PackSize, decimal PriceEuro);

public record TokenSpendCostDto(string Reason, decimal CostTokens);

/// <param name="PaymentMethod">Mollie method: ideal or creditcard. Falls back to company preference, then iDEAL.</param>
public record CreateCheckoutRequest(
    Guid CompanyId,
    int PackSize,
    PendingActionCheckoutRequest? PendingAction = null,
    string? PaymentMethod = null);

/// <summary>Deferred vacancy product action executed after Mollie fulfillment.</summary>
public record PendingActionCheckoutRequest(
    Guid VacancyId,
    string Action,
    bool Highlight = false,
    bool PushBom = false,
    bool Extend = false,
    decimal? RequiredTokens = null);

public record CheckoutResultDto(
    string PaymentId,
    string CheckoutUrl,
    int PackSize,
    decimal AmountEuro,
    bool IsStub,
    Guid CheckoutId = default,
    string? PaymentMethod = null);

public record CompleteCheckoutRequest(string? PaymentId = null, Guid? CheckoutId = null);

public record CompleteCheckoutResultDto(
    Guid CompanyId,
    string CompanyName,
    decimal Balance,
    Guid CheckoutId,
    PendingActionResultDto? PendingAction = null);

public record PendingActionResultDto(
    Guid VacancyId,
    string Action,
    bool Succeeded,
    string? Message,
    int PushBomRecipientCount = 0);

public record TokenTopUpQuoteDto(
    Guid CompanyId,
    decimal Balance,
    decimal RequiredTokens,
    decimal Deficit,
    int ExactMatchTokens,
    decimal ExactMatchPriceEuro,
    IReadOnlyList<TokenPackDto> BulkPacks);

public record AllocateTokensRequest(Guid FromCompanyId, Guid ToCompanyId, decimal Amount, string? Note = null);

public record RegionDto(
    Guid Id,
    string Name,
    Guid OrganizationCompanyId,
    string OrganizationCompanyName,
    IReadOnlyList<RegionCompanyItemDto> Companies);

public record RegionCompanyItemDto(Guid CompanyId, string CompanyName);

public record CreateRegionRequest(string Name, Guid OrganizationCompanyId, Guid[]? CompanyIds = null);

public record UpdateRegionRequest(string Name, Guid[] CompanyIds);

public record SalaryTableDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Name,
    bool IsActive,
    bool IsSystemWml,
    int VacancyCount,
    IReadOnlyList<Guid> AllowedBranchIds,
    IReadOnlyList<string> AllowedBranchNames,
    IReadOnlyList<SalaryRateDto> Rates,
    IReadOnlyList<SalaryTableChangeLogDto>? ChangeLogs = null);

public record SalaryRateDto(Guid Id, int AgeYears, decimal HourlyRate, string Label);

public record SalaryTableChangeLogDto(
    Guid Id,
    string Action,
    string? ActorEmail,
    string Message,
    DateTime CreatedAt);

public record SalaryTableVacancyDto(
    Guid Id,
    string Title,
    string CompanyName,
    string Status);

public record UpsertSalaryTableRequest(
    Guid? Id,
    Guid CompanyId,
    string Name,
    bool IsActive,
    IReadOnlyList<UpsertSalaryRateRequest>? Rates = null,
    IReadOnlyList<Guid>? AllowedBranchIds = null);

public record UpsertSalaryRateRequest(Guid? Id, int AgeYears, decimal HourlyRate, string Label);

public record RegisterEstablishmentRequest(string KvkNumber, string KvkEstablishmentId, Guid? ParentCompanyId = null);

public record CompanyUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    string? CompanyName,
    IReadOnlyList<Guid> MembershipCompanyIds,
    bool IsActive = true,
    string? TemporaryPassword = null,
    string? LoginUrl = null);

public record InviteUserRequest(
    string Email,
    string FullName,
    UserRole Role,
    Guid? PrimaryCompanyId,
    Guid[]? MembershipCompanyIds = null,
    Guid? RegionId = null);

public record UpdateCompanyUserRequest(
    string FullName,
    UserRole Role,
    Guid? PrimaryCompanyId,
    Guid[]? MembershipCompanyIds = null,
    bool IsActive = true);

public record EmployerApplicationDto(
    Guid Id,
    Guid VacancyId,
    string VacancyTitle,
    string CompanyName,
    string PreferredTransport,
    int EstimatedTravelMinutes,
    DateTime CreatedAt,
    string Status,
    DateTime? RespondedAt,
    string? CandidateCity,
    /// <summary>Crow-flies distance — visible before Accept (no address/city).</summary>
    double? DistanceKm,
    string? PreferencesSummary,
    /// <summary>Null until Accepted — progressive disclosure.</summary>
    string? CandidateName,
    /// <summary>Null until Hired — direct contact stays hidden until then.</summary>
    string? CandidateEmail,
    /// <summary>Null until Accepted — progressive disclosure.</summary>
    string? CandidateAddress,
    bool PiiRevealed,
    bool WorkPermitConfirmed,
    string? SnapshotAvailabilityJson,
    string? SnapshotDrivingLicenses,
    string? SnapshotEducations,
    string? SnapshotAboutMe,
    int CandidateEmployerCount,
    int? MatchPercent = null,
    string? MatchBreakdownJson = null,
    bool ViaSafetyNet = false,
    string? Motivation = null,
    bool LegalEligible = true,
    /// <summary>Null until Accepted — progressive disclosure (stage exclusivity).</summary>
    string? StudentNumber = null,
    /// <summary>Null until Hired — school e-mail is direct contact.</summary>
    string? SchoolEmail = null,
    /// <summary>Null until Accepted — progressive disclosure (stage exclusivity).</summary>
    string? StudyProgram = null,
    /// <summary>Null until Accepted — progressive disclosure (stage exclusivity).</summary>
    string? StudyYear = null,
    /// <summary>Null until Accepted — progressive disclosure (stage exclusivity).</summary>
    string? ExclusivityValidationStatus = null,
    /// <summary>True when employer may download Lobsy-CV PDF (= PiiRevealed).</summary>
    bool CvPdfAvailable = false,
    /// <summary>Null until Hired — phone from apply snapshot.</summary>
    string? CandidatePhone = null,
    /// <summary>WhatsApp contact allowed (only meaningful when CandidatePhone is revealed).</summary>
    bool WhatsAppContactAllowed = false,
    /// <summary>Age at apply — visible before Accept.</summary>
    int? CandidateAgeYears = null,
    /// <summary>Formatted availability — visible before Accept.</summary>
    string? AvailabilitySummary = null,
    /// <summary>True when employer may download the candidate-uploaded CV (post-Accept snapshot).</summary>
    bool UploadedCvAvailable = false,
    int CandidateReferenceCount = 0);

public record FulfillVacancyRequest(
    bool RejectOtherApplications = true,
    bool CloseVacancy = true);
