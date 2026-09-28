using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer/candidate-insights")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class CandidateInsightsController : ControllerBase
{
    private static readonly HashSet<string> ForbiddenIdentityParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "minAge", "maxAge", "age", "dateOfBirth", "userId", "candidateId", "candidateUserId"
    };

    private readonly ICandidateInsightsService _insights;

    public CandidateInsightsController(ICandidateInsightsService insights)
    {
        _insights = insights;
    }

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<CandidateInsightsDto>> Get(
        [FromQuery] Guid? branchId,
        [FromQuery] int radiusKm = 20,
        [FromQuery] int period = 90,
        CancellationToken cancellationToken = default)
    {
        if (HasForbiddenIdentityQuery())
        {
            return BadRequest(new { message = "Identiteits- of leeftijdsfilters zijn niet toegestaan." });
        }

        if (!CandidateInsightsService.AllowedRadiiKm.Contains(radiusKm)
            || !CandidateInsightsService.AllowedPeriodsDays.Contains(period))
        {
            return BadRequest(new { message = "Ongeldige radius of periode." });
        }

        if (!IsInsightsRole())
        {
            return Forbid();
        }

        try
        {
            var dto = await _insights.GetInsightsAsync(User, branchId, radiusKm, period, cancellationToken);
            return Ok(dto);
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("branches")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<CandidateInsightsBranchDto>>> Branches(
        CancellationToken cancellationToken = default)
    {
        if (!IsInsightsRole())
        {
            return Forbid();
        }

        try
        {
            return Ok(await _insights.GetBranchesAsync(User, cancellationToken));
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
    }

    private bool IsInsightsRole()
        => User.IsInRole(JobsyRoles.BranchManager)
           || User.IsInRole(JobsyRoles.RegionalManager)
           || User.IsInRole(JobsyRoles.EnterpriseManager)
           || RoleClaimMatching.HasRole(User, JobsyRoles.BranchManager)
           || RoleClaimMatching.HasRole(User, JobsyRoles.RegionalManager)
           || RoleClaimMatching.HasRole(User, JobsyRoles.EnterpriseManager);

    private bool HasForbiddenIdentityQuery()
        => Request.Query.Keys.Any(k => ForbiddenIdentityParams.Contains(k));
}
