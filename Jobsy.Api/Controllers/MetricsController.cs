using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/metrics")]
[Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
public class MetricsController : ControllerBase
{
    private readonly IMetricsQueryService _metrics;
    private readonly ICompanyAuthorizationService _companyAuth;

    public MetricsController(IMetricsQueryService metrics, ICompanyAuthorizationService companyAuth)
    {
        _metrics = metrics;
        _companyAuth = companyAuth;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<MetricCountDto>>> GetSummary(
        [FromQuery] string period = "day",
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var companyIds = await ResolveCompanyFilterAsync(companyId, cancellationToken);
        if (companyIds is not null && companyIds.Count == 0)
        {
            return Forbid();
        }

        var includePlatformOnly = _companyAuth.IsAdmin(User);
        var metrics = await _metrics.GetSummaryAsync(includePlatformOnly, companyIds, period, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("drilldown/{key}")]
    public async Task<ActionResult<IEnumerable<MetricDrilldownItemDto>>> GetDrilldown(
        string key,
        [FromQuery] string period = "day",
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var includePlatformOnly = _companyAuth.IsAdmin(User);
        if (!includePlatformOnly && MetricsKeys.PlatformOnly.Contains(key))
        {
            return Forbid();
        }

        var companyIds = await ResolveCompanyFilterAsync(companyId, cancellationToken);
        if (companyIds is not null && companyIds.Count == 0)
        {
            return Forbid();
        }

        var items = await _metrics.GetDrilldownAsync(key, includePlatformOnly, companyIds, period, cancellationToken);
        return Ok(items);
    }

    [HttpGet("vacancy-performance")]
    public async Task<ActionResult<VacancyPerformanceBoardDto>> GetVacancyPerformance(
        [FromQuery] string period = "week",
        [FromQuery] Guid? companyId = null,
        [FromQuery] int take = 3,
        CancellationToken cancellationToken = default)
    {
        var companyIds = await ResolveCompanyFilterAsync(companyId, cancellationToken);
        if (companyIds is not null && companyIds.Count == 0)
        {
            return Forbid();
        }

        var board = await _metrics.GetVacancyPerformanceAsync(companyIds, period, take, cancellationToken);
        return Ok(board);
    }

    private async Task<IReadOnlyCollection<Guid>?> ResolveCompanyFilterAsync(
        Guid? companyId,
        CancellationToken ct)
    {
        if (_companyAuth.IsAdmin(User) && companyId is null)
        {
            return null;
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, ct);
        if (companyId is not null)
        {
            if (accessible is not null && !accessible.Contains(companyId.Value) && !_companyAuth.IsAdmin(User))
            {
                return Array.Empty<Guid>();
            }

            return [companyId.Value];
        }

        return accessible;
    }
}
