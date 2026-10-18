using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer/kandidatenkaart")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
[RequiresFeature(PlatformFeature.Employers)]
[RequiresFeature(PlatformFeature.Kandidatenkaart)]
public sealed class KandidatenkaartController : ControllerBase
{
    private readonly IKandidatenkaartService _service;
    private readonly ICompanyAuthorizationService _authz;

    public KandidatenkaartController(IKandidatenkaartService service, ICompanyAuthorizationService authz)
    {
        _service = service;
        _authz = authz;
    }

    [HttpGet("access")]
    [EnableRateLimiting("public-read")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<KandidatenkaartAccessDto>> Access(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetAccessAsync(User, companyId, cancellationToken));
        }
        catch (KandidatenkaartException ex) when (ex.Code == "feature_disabled")
        {
            return NotFound(new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpGet("vacancies")]
    [EnableRateLimiting("public-read")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<IReadOnlyList<KandidatenkaartVacancyOptionDto>>> Vacancies(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var ids = await ResolveCompanyIdsAsync(companyId, cancellationToken);
        return Ok(await _service.ListVacanciesAsync(User, ids, cancellationToken));
    }

    [HttpGet("map")]
    [EnableRateLimiting("public-read")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<KandidatenkaartMapDto>> Map(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? vacancyId,
        [FromQuery] int minMatch = 70,
        [FromQuery] int travelKm = 25,
        [FromQuery] string? availability = null,
        [FromQuery] string? hours = null,
        CancellationToken cancellationToken = default)
    {
        var ids = await ResolveCompanyIdsAsync(companyId, cancellationToken);
        try
        {
            return Ok(await _service.GetMapAsync(User, ids, vacancyId, minMatch, travelKm, availability, hours, cancellationToken));
        }
        catch (KandidatenkaartException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpGet("invite/preview")]
    [EnableRateLimiting("public-read")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<KandidatenkaartInvitePreviewDto>> PreviewInvite(
        [FromQuery] Guid vacancyId,
        [FromQuery] string areaKey,
        [FromQuery] Guid? companyId,
        [FromQuery] int minMatch = 70,
        [FromQuery] int travelKm = 25,
        CancellationToken cancellationToken = default)
    {
        var ids = await ResolveCompanyIdsAsync(companyId, cancellationToken);
        try
        {
            return Ok(await _service.PreviewInviteAsync(User, ids, vacancyId, areaKey, minMatch, travelKm, cancellationToken));
        }
        catch (KandidatenkaartException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("invite")]
    [Authorize(Roles = JobsyRoles.EmployerMutateRoles)]
    [EnableRateLimiting("authenticated-write")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<KandidatenkaartInviteResultDto>> StartInvite(
        [FromBody] KandidatenkaartInviteRequest body,
        CancellationToken cancellationToken)
    {
        var ids = await ResolveCompanyIdsAsync(body.CompanyId, cancellationToken);
        try
        {
            return Ok(await _service.StartInviteCampaignAsync(
                User, ids, body.VacancyId, body.AreaKey, body.MinMatchPercent, body.TravelKm, cancellationToken));
        }
        catch (KandidatenkaartException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpGet("campaigns")]
    [EnableRateLimiting("public-read")]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<IReadOnlyList<KandidatenkaartCampaignStatusDto>>> Campaigns(
        [FromQuery] Guid vacancyId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ListCampaignsAsync(User, vacancyId, cancellationToken));
        }
        catch (KandidatenkaartException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    private async Task<IReadOnlyList<Guid>> ResolveCompanyIdsAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is Guid id)
        {
            return [id];
        }

        var list = await _authz.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        return list?.ToList() ?? [];
    }
}

[ApiController]
[Route("api/me/kandidatenkaart")]
[Authorize(Roles = JobsyRoles.Candidate)]
[RequiresFeature(PlatformFeature.Kandidatenkaart)]
public sealed class CandidateKandidatenkaartController : ControllerBase
{
    private readonly IKandidatenkaartService _service;
    private readonly IUserLookupService _users;

    public CandidateKandidatenkaartController(IKandidatenkaartService service, IUserLookupService users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<CandidateKandidatenkaartSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _service.GetCandidateSettingsAsync(user.Id, cancellationToken));
        }
        catch (KandidatenkaartException ex) when (ex.Code == "feature_disabled")
        {
            return NotFound();
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<CandidateKandidatenkaartSettingsDto>> UpdateSettings(
        [FromBody] CandidateKandidatenkaartSettingsUpdate body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _service.UpdateCandidateSettingsAsync(user.Id, body, cancellationToken));
    }

    [HttpPost("re-availability")]
    public async Task<IActionResult> ReAvailability(
        [FromBody] KandidatenkaartReAvailabilityAck body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await _service.AckReAvailabilityAsync(user.Id, body, cancellationToken);
        return NoContent();
    }
}

public sealed record KandidatenkaartInviteRequest(
    Guid VacancyId,
    string AreaKey,
    Guid? CompanyId,
    int MinMatchPercent = 70,
    int TravelKm = 25);
