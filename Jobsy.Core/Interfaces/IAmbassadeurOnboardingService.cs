using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IAmbassadeurOnboardingService
{
    Task<AmbassadeurProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AmbassadeurProfileDto> UpdateProfileAsync(
        Guid userId,
        AmbassadeurProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<AmbassadeurProfileDto> SignAgreementAsync(
        Guid userId,
        string agreementVersion,
        CancellationToken cancellationToken = default);
}

public sealed record AmbassadeurProfileUpdateRequest(
    string CompanyName,
    string KvkNumber,
    string VatNumber,
    string Address,
    string PostalCode,
    string City,
    string? Country = "NL",
    string? Iban = null);

public sealed record AmbassadeurProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string? CompanyName,
    string? KvkNumber,
    string? VatNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? Iban,
    string? TrackingCode,
    decimal BaseCommissionPercentage,
    decimal CurrentCommissionPercentage,
    decimal MaxCommissionPercentage,
    decimal? CommissionPercentageOverride,
    DateTime? AgreementSignedAt,
    string? AgreementVersion,
    DateTime? OnboardingCompletedAt,
    bool IsOnboardingComplete);
