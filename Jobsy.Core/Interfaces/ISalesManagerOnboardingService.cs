using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ISalesManagerOnboardingService
{
    Task<SalesManagerProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<SalesManagerProfileDto> UpdateProfileAsync(
        Guid userId,
        SalesManagerProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<SalesManagerProfileDto> SignAgreementAsync(
        Guid userId,
        string agreementVersion,
        CancellationToken cancellationToken = default);
}

public sealed record SalesManagerProfileUpdateRequest(
    string CompanyName,
    string KvkNumber,
    string VatNumber,
    string Address,
    string PostalCode,
    string City,
    string? Country = "NL",
    string? Iban = null);

public sealed record SalesManagerProfileDto(
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
    DateTime? AgreementSignedAt,
    string? AgreementVersion,
    DateTime? OnboardingCompletedAt,
    bool IsOnboardingComplete,
    bool CanRecruitSalesManagers = true,
    Guid? ReferredBySalesManagerUserId = null);
