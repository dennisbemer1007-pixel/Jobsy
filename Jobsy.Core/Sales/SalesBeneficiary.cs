using System.Security.Claims;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

public sealed record SalesBeneficiary(
    Guid UserId,
    SalesBeneficiaryKind Kind,
    string? TrackingCode,
    bool IsOnboardingComplete,
    bool CanRecruit);

public interface ISalesBeneficiaryService
{
    Task<SalesBeneficiary> GetOrThrowAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<bool> CanSeeCompanyAsync(
        Guid beneficiaryUserId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<bool> CanSeeInvoiceAsync(
        Guid beneficiaryUserId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<bool> CanSeePayoutRequestAsync(
        Guid beneficiaryUserId,
        Guid payoutRequestId,
        CancellationToken cancellationToken = default);
}

public sealed record SalesParkedBalanceItem(
    Guid UserId,
    string MaskedDisplayName,
    decimal OpenBalanceExVat,
    DateTime? LastLineAtUtc);

public interface ISalesParkedBalanceService
{
    Task<IReadOnlyList<SalesParkedBalanceItem>> ListAsync(CancellationToken cancellationToken = default);
}
