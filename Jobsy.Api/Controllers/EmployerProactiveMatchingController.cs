using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer/proactive-matching")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
[RequiresFeature(PlatformFeature.ProactiveMatching)]
[RequiresFeature(PlatformFeature.Employers)]
public sealed class EmployerProactiveMatchingController : ControllerBase
{
    private readonly IProactiveVacancyShortlistService _shortlist;
    private readonly ITalentPoolService _talent;
    private readonly IUserLookupService _users;
    private readonly ICompanyAuthorizationService _authz;
    public EmployerProactiveMatchingController(
        IProactiveVacancyShortlistService shortlist,
        ITalentPoolService talent,
        IUserLookupService users,
        ICompanyAuthorizationService authz)
    {
        _shortlist = shortlist;
        _talent = talent;
        _users = users;
        _authz = authz;
    }

    [HttpGet("vacancies/{vacancyId:guid}/shortlist")]
    [EnableRateLimiting("public-read")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<IReadOnlyList<ProactiveVacancyShortlistEntryDto>>> Shortlist(
        Guid vacancyId,
        CancellationToken cancellationToken)
    {
        var (companyId, _) = await ResolveCompanyAsync(cancellationToken);
        if (companyId is null)
        {
            return Forbid();
        }

        try
        {
            return Ok(await _shortlist.BuildShortlistAsync(companyId.Value, vacancyId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Vacature niet gevonden." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("vacancies/{vacancyId:guid}/introduce")]
    [Authorize(Roles = JobsyRoles.EmployerMutateRoles)]
    [EnableRateLimiting("public-write")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<TalentContactRequestDto>> Introduce(
        Guid vacancyId,
        [FromBody] ProactiveIntroduceRequest body,
        CancellationToken cancellationToken)
    {
        var (companyId, user) = await ResolveCompanyAsync(cancellationToken);
        if (companyId is null || user is null)
        {
            return Forbid();
        }

        try
        {
            var dto = await _talent.SendVacancyIntroductionAsync(
                companyId.Value,
                user.Id,
                vacancyId,
                body.CandidateUserId,
                body.Message ?? "",
                cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<(Guid? CompanyId, Jobsy.Core.Entities.User? User)> ResolveCompanyAsync(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return (null, null);
        }

        var accessible = await _authz.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is null || accessible.Count == 0)
        {
            return (null, user);
        }

        var companyId = user.CompanyId is Guid home && accessible.Contains(home)
            ? home
            : accessible.First();
        return (companyId, user);
    }
}

public sealed record ProactiveIntroduceRequest(Guid CandidateUserId, string? Message);
