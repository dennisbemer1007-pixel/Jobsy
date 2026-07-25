using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record TokenPackDto(int PackSize, decimal PriceEuro);

public record TokenSpendCostDto(string Reason, decimal CostTokens);

public record CreateCheckoutRequest(Guid CompanyId, int PackSize);

public record CheckoutResultDto(
    string PaymentId,
    string CheckoutUrl,
    int PackSize,
    decimal AmountEuro,
    bool IsStub);

public record CompleteCheckoutRequest(string PaymentId);

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
    IReadOnlyList<SalaryRateDto> Rates);

public record SalaryRateDto(Guid Id, int AgeYears, decimal HourlyRate, string Label);

public record UpsertSalaryTableRequest(
    Guid? Id,
    Guid CompanyId,
    string Name,
    bool IsActive,
    IReadOnlyList<UpsertSalaryRateRequest>? Rates = null);

public record UpsertSalaryRateRequest(Guid? Id, int AgeYears, decimal HourlyRate, string Label);

public record RegisterEstablishmentRequest(string KvkNumber, string KvkEstablishmentId, Guid? ParentCompanyId = null);

public record CompanyUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    string? CompanyName,
    IReadOnlyList<Guid> MembershipCompanyIds);

public record InviteUserRequest(
    string Email,
    string FullName,
    UserRole Role,
    Guid? PrimaryCompanyId,
    Guid[]? MembershipCompanyIds = null);

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
    double? DistanceKm,
    string? PreferencesSummary,
    /// <summary>Null until Accepted — progressive disclosure.</summary>
    string? CandidateName,
    /// <summary>Null until Accepted — progressive disclosure.</summary>
    string? CandidateEmail,
    /// <summary>Null until Accepted — progressive disclosure.</summary>
    string? CandidateAddress,
    bool PiiRevealed);
