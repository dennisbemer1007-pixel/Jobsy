using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/werkgever")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class WerkgeverDashboardController : ControllerBase
{
    private readonly IWerkgeverDashboardService _dashboard;
    private readonly IWerkgeverTokenSummaryService _tokenSummary;
    private readonly ICompanyAuthorizationService _companyAuth;

    public WerkgeverDashboardController(
        IWerkgeverDashboardService dashboard,
        IWerkgeverTokenSummaryService tokenSummary,
        ICompanyAuthorizationService companyAuth)
    {
        _dashboard = dashboard;
        _tokenSummary = tokenSummary;
        _companyAuth = companyAuth;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(WerkgeverDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WerkgeverDashboardDto>> GetDashboard(
        [FromQuery] string? period,
        [FromQuery] List<Guid>? companyIds,
        CancellationToken cancellationToken)
    {
        var scoped = await ResolveScopeAsync(companyIds, cancellationToken);
        if (scoped is null)
        {
            return Forbid();
        }

        var role = ResolveRole(User);
        var dto = await _dashboard.GetDashboardAsync(
            scoped,
            WerkgeverDashboardRules.NormalizePeriod(period),
            role,
            cancellationToken);
        return Ok(dto);
    }

    [HttpGet("te-doen")]
    [ProducesResponseType(typeof(IReadOnlyList<WerkgeverTodoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<WerkgeverTodoItemDto>>> GetTodo(
        [FromQuery] List<Guid>? companyIds,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var scoped = await ResolveScopeAsync(companyIds, cancellationToken);
        if (scoped is null)
        {
            return Forbid();
        }

        var role = ResolveRole(User);
        var items = await _dashboard.GetTodoAsync(
            scoped,
            role,
            take ?? WerkgeverDashboardRules.TodoDefaultTake,
            cancellationToken);
        return Ok(items);
    }

    [HttpGet("tokens/summary")]
    [ProducesResponseType(typeof(WerkgeverTokenSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WerkgeverTokenSummaryDto>> GetTokenSummary(
        [FromQuery] List<Guid>? companyIds,
        [FromQuery] int? periodDays,
        CancellationToken cancellationToken)
    {
        var scoped = await ResolveScopeAsync(companyIds, cancellationToken);
        if (scoped is null)
        {
            return Forbid();
        }

        var summary = await _tokenSummary.GetSummaryAsync(
            scoped,
            periodDays ?? 30,
            cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Intersects requested companyIds with accessible ids. Empty intersection → null (403).
    /// Empty request never means "all".
    /// </summary>
    private async Task<IReadOnlyList<Guid>?> ResolveScopeAsync(
        List<Guid>? requested,
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is null)
        {
            // Admin via RequireEmployer is unlikely; treat null as no employer scope.
            return null;
        }

        if (accessible.Count == 0)
        {
            return null;
        }

        if (requested is null || requested.Count == 0)
        {
            // Client must send scope; empty never means all.
            return null;
        }

        var intersection = requested.Where(accessible.Contains).Distinct().ToList();
        return intersection.Count == 0 ? null : intersection;
    }

    private static WerkgeverDashboardRole ResolveRole(ClaimsPrincipal user)
    {
        if (user.IsInRole(JobsyRoles.EnterpriseManager))
        {
            return WerkgeverDashboardRole.Bedrijfsmanager;
        }

        if (user.IsInRole(JobsyRoles.RegionalManager))
        {
            return WerkgeverDashboardRole.Regiomanager;
        }

        if (user.IsInRole(JobsyRoles.BranchManager))
        {
            return WerkgeverDashboardRole.Vestigingsmanager;
        }

        if (user.IsInRole(JobsyRoles.Intermediary))
        {
            return WerkgeverDashboardRole.Intermediair;
        }

        return WerkgeverDashboardRole.Bedrijfsmanager;
    }
}
