using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

/// <summary>Self-service portal APIs for the signed-in sales beneficiary (SalesManager).</summary>
[ApiController]
[Route("api/sales/me")]
[Authorize(Roles = JobsyRoles.SalesManager)]
public sealed class SalesMeController : ControllerBase
{
    private readonly ISalesBeneficiaryService _beneficiary;
    private readonly ISalesDashboardReadService _dashboard;
    private readonly ISalesEmployerPortalReadService _employers;

    public SalesMeController(
        ISalesBeneficiaryService beneficiary,
        ISalesDashboardReadService dashboard,
        ISalesEmployerPortalReadService employers)
    {
        _beneficiary = beneficiary;
        _dashboard = dashboard;
        _employers = employers;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<SalesDashboardDto>> GetDashboard(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _dashboard.GetAsync(me.UserId, period ?? "year", cancellationToken: cancellationToken);
        return Ok(dto);
    }

    [HttpGet("employers")]
    public async Task<ActionResult<SalesEmployerPageDto>> ListEmployers(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        SalesEmployerStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<SalesEmployerStatus>(status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var dto = await _employers.ListAsync(
            me.UserId,
            q,
            statusFilter,
            year,
            page,
            cancellationToken: cancellationToken);
        return Ok(dto);
    }

    [HttpGet("employers/{companyId:guid}")]
    public async Task<ActionResult<SalesEmployerDetailDto>> GetEmployer(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _employers.GetDetailAsync(me.UserId, companyId, cancellationToken: cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }
}
