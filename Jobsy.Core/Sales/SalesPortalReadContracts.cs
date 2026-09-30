using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

public interface ISalesDashboardReadService
{
    Task<SalesDashboardDto> GetAsync(
        Guid beneficiaryUserId,
        string period,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);
}

public interface ISalesEmployerPortalReadService
{
    Task<SalesEmployerPageDto> ListAsync(
        Guid beneficiaryUserId,
        string? query,
        SalesEmployerStatus? status,
        int? commissionYear,
        int page,
        int pageSize = 25,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    Task<SalesEmployerDetailDto?> GetDetailAsync(
        Guid beneficiaryUserId,
        Guid companyId,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);
}
