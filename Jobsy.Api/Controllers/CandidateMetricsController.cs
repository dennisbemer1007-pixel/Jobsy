using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
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
    private readonly IFeatureFlags _featureFlags;

    public CandidateMetricsController(
        ICandidateMetricsQueryService metrics,
        IUserLookupService users,
        IFeatureFlags featureFlags)
    {
        _metrics = metrics;
        _users = users;
        _featureFlags = featureFlags;
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

        if (!await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken))
        {
            // Vacancy tiles (applications/likes/shares/reactions) stay hidden when employers are OFF.
            return Ok(Array.Empty<MetricCountDto>());
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

        if (!await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken))
        {
            return Ok(Array.Empty<MetricDrilldownItemDto>());
        }

        var items = await _metrics.GetDrilldownAsync(user.Id, key, period, cancellationToken);
        return Ok(items);
    }
}
