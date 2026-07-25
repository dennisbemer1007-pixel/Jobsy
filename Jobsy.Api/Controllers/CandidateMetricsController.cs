using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/metrics")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public class CandidateMetricsController : ControllerBase
{
    private readonly ICandidateMetricsQueryService _metrics;
    private readonly IUserLookupService _users;

    public CandidateMetricsController(ICandidateMetricsQueryService metrics, IUserLookupService users)
    {
        _metrics = metrics;
        _users = users;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<MetricCountDto>>> GetSummary(
        [FromQuery] string period = "week",
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var metrics = await _metrics.GetSummaryAsync(user.Id, period, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("drilldown/{key}")]
    public async Task<ActionResult<IEnumerable<MetricDrilldownItemDto>>> GetDrilldown(
        string key,
        [FromQuery] string period = "week",
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var items = await _metrics.GetDrilldownAsync(user.Id, key, period, cancellationToken);
        return Ok(items);
    }
}
