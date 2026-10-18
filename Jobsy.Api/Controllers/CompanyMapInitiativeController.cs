using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Api.Authorization;
using Jobsy.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/company-map-initiative")]
public sealed class CompanyMapInitiativeController : ControllerBase
{
    private readonly ICompanyMapInitiativeService _service;

    public CompanyMapInitiativeController(ICompanyMapInitiativeService service)
    {
        _service = service;
    }

    [HttpGet("public/{kvk}/profile")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<PublicCompanyInitiativeViewDto>> GetPublicProfile(
        string kvk,
        CancellationToken cancellationToken)
    {
        Guid? candidateId = null;
        if (User.Identity?.IsAuthenticated == true
            && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
            && RoleClaimMatching.HasRole(User, JobsyRoles.Candidate))
        {
            candidateId = uid;
        }

        var profile = await _service.GetPublicProfileByKvkAsync(kvk, candidateId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("pins")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<IReadOnlyList<CompanyMapPinDto>>> GetPins(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] int radiusKm = 25,
        CancellationToken cancellationToken = default)
    {
        Guid? candidateId = null;
        if (User.Identity?.IsAuthenticated == true
            && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
            && RoleClaimMatching.HasRole(User, JobsyRoles.Candidate))
        {
            candidateId = uid;
        }

        var pins = await _service.GetMapPinsAsync(lat, lng, radiusKm, candidateId, cancellationToken);
        return Ok(pins);
    }

    [HttpGet("candidate/week")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<CandidateInitiativeWeekDto>> GetWeek(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _service.GetCandidateWeekAsync(userId, cancellationToken));
    }

    [HttpPost("candidate/send")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<SendWorkInitiativeResult>> Send(
        [FromBody] SendWorkInitiativeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _service.SendAsync(userId, request, cancellationToken));
    }

    [HttpPost("candidate/drafts/{draftId:guid}/send")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<SendWorkInitiativeResult>> SendDraft(Guid draftId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _service.SendDraftAsync(userId, draftId, cancellationToken));
    }

    [HttpGet("candidate/list")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<IReadOnlyList<WorkInitiativeListItemDto>>> CandidateList(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _service.ListForCandidateAsync(userId, cancellationToken));
    }

    [HttpPost("candidate/{id:guid}/withdraw")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.WithdrawAsync(userId, id, cancellationToken);
        return NoContent();
    }

    [HttpGet("employer/{companyId:guid}/inbox")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<IReadOnlyList<EmployerInitiativeInboxItemDto>>> EmployerInbox(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.ListForEmployerAsync(companyId, cancellationToken));
    }

    [HttpGet("employer/{rootCompanyId:guid}/settings")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<CompanyInitiativeSettingsDto>> GetSettings(
        Guid rootCompanyId,
        CancellationToken cancellationToken)
    {
        var dto = await _service.GetSettingsAsync(rootCompanyId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("employer/{rootCompanyId:guid}/settings")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<CompanyInitiativeSettingsDto>> SaveSettings(
        Guid rootCompanyId,
        [FromBody] CompanyInitiativeSettingsDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.SaveSettingsAsync(rootCompanyId, dto, cancellationToken));
    }

    [HttpPut("employer/{rootCompanyId:guid}/teams")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<ActionResult<InitiativeTeamDto>> SaveTeam(
        Guid rootCompanyId,
        [FromBody] InitiativeTeamDto team,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.SaveTeamAsync(rootCompanyId, team, cancellationToken));
        }
        catch (InvalidOperationException ex) when (ex.Message == "team_email_required")
        {
            return BadRequest(new { message = "team_email_required" });
        }
    }

    [HttpPost("employer/{rootCompanyId:guid}/teams/{teamId:guid}/resend-email")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<IActionResult> ResendTeamEmail(Guid rootCompanyId, Guid teamId, CancellationToken cancellationToken)
    {
        await _service.ResendTeamEmailConfirmationAsync(rootCompanyId, teamId, cancellationToken);
        return NoContent();
    }

    [HttpPost("employer/{rootCompanyId:guid}/confirm-email")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<IActionResult> ConfirmGeneralEmail(
        Guid rootCompanyId,
        [FromBody] ConfirmInitiativeEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.ConfirmGeneralEmailAsync(rootCompanyId, request.Code, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "code_invalid")
        {
            return BadRequest(new { message = "code_invalid" });
        }
    }

    [HttpPost("employer/teams/{teamId:guid}/confirm-email")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<IActionResult> ConfirmTeamEmail(
        Guid teamId,
        [FromBody] ConfirmInitiativeEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.ConfirmTeamEmailAsync(teamId, request.Code, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "code_invalid")
        {
            return BadRequest(new { message = "code_invalid" });
        }
    }

    [HttpPost("employer/{id:guid}/accept")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromQuery] Guid billingCompanyId,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.AcceptAsync(userId, id, billingCompanyId, cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { code = result.ErrorCode, balance = result.Balance });
        }

        return Ok(new { balance = result.Balance });
    }

    [HttpPost("employer/{id:guid}/decline")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<IActionResult> Decline(
        Guid id,
        [FromBody] DeclineInitiativeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.DeclineAsync(
            userId,
            id,
            request.KeepOpen,
            request.ReducedMaxPerWeek,
            request.CloseTeam,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("employer/{id:guid}/viewed")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequiresFeature(PlatformFeature.CompanyMapInitiative)]
    public async Task<IActionResult> MarkViewed(Guid id, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.MarkViewedAsync(userId, id, cancellationToken);
        return NoContent();
    }
}

public sealed record ConfirmInitiativeEmailRequest(string Code);

public sealed record DeclineInitiativeRequest(
    bool KeepOpen,
    int? ReducedMaxPerWeek,
    bool CloseTeam);
