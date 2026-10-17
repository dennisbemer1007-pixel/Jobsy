using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/research")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class AssessmentResearchController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IAssessmentResearchService _research;
    private readonly IUserLookupService _users;

    public AssessmentResearchController(
        JobsyDbContext db,
        IAssessmentResearchService research,
        IUserLookupService users)
    {
        _db = db;
        _research = research;
        _users = users;
    }

    [HttpGet("consent")]
    public async Task<ActionResult<ResearchConsentStateDto>> GetConsent(CancellationToken cancellationToken)
    {
        if (!await _research.IsFeatureEnabledAsync(cancellationToken))
        {
            return Ok(new ResearchConsentStateDto(false, null, null, null, null, null, null, null));
        }

        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == lookup.Id, cancellationToken);
        return Ok(ToState(user));
    }

    [HttpPost("consent")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ResearchConsentStateDto>> AcceptConsent(
        [FromBody] AcceptResearchConsentRequest request,
        CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        await _research.AcceptResearchConsentAsync(
            lookup.Id,
            request.Demographics is null
                ? null
                : new ResearchDemographicsDto(
                    request.Demographics.AgeBand,
                    request.Demographics.Gender,
                    request.Demographics.EducationLevel,
                    request.Demographics.NativeLanguage,
                    request.Demographics.TestLanguage),
            cancellationToken);

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == lookup.Id, cancellationToken);
        return Ok(ToState(user));
    }

    [HttpDelete("consent")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ResearchConsentStateDto>> WithdrawConsent(CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        await _research.WithdrawResearchConsentAsync(lookup.Id, cancellationToken);
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == lookup.Id, cancellationToken);
        return Ok(ToState(user));
    }

    [HttpPut("demographics")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ResearchConsentStateDto>> UpdateDemographics(
        [FromBody] ResearchDemographicsRequest request,
        CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        await _research.UpdateDemographicsAsync(
            lookup.Id,
            new ResearchDemographicsDto(
                request.AgeBand,
                request.Gender,
                request.EducationLevel,
                request.NativeLanguage,
                request.TestLanguage),
            cancellationToken);
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == lookup.Id, cancellationToken);
        return Ok(ToState(user));
    }

    private static ResearchConsentStateDto ToState(Core.Entities.User user)
        => new(
            HasConsent: ResearchConsentRules.HasCurrentResearchConsent(user),
            user.ResearchConsentAt,
            user.ResearchConsentVersion,
            user.ResearchAgeBand,
            user.ResearchGender,
            user.ResearchEducationLevel,
            user.ResearchNativeLanguage,
            user.ResearchTestLanguage);
}

[ApiController]
[Route("api/public/research")]
public sealed class ResearchPilotController : ControllerBase
{
    private readonly IAssessmentResearchService _research;

    public ResearchPilotController(IAssessmentResearchService research)
    {
        _research = research;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ActionResult<ResearchFeatureStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var enabled = await _research.IsFeatureEnabledAsync(cancellationToken);
        return Ok(new ResearchFeatureStatusDto(
            enabled,
            PrivacyConstants.ResearchConsentTextVersion,
            PrivacyConstants.ResearchExportTargetRespondents));
    }

    [HttpPost("pilot/{code}/start")]
    [AllowAnonymous]
    public async Task<ActionResult<ResearchPilotStartDto>> Start(
        string code,
        [FromBody] StartPilotRequest? request,
        CancellationToken cancellationToken)
    {
        var started = await _research.StartPilotAsync(code, request?.TestLanguage, cancellationToken);
        return started is null ? NotFound() : Ok(started);
    }

    [HttpPost("sessions/{sessionId:guid}/attempts")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitAttempt(
        Guid sessionId,
        [FromBody] SubmitPilotAttemptRequest request,
        CancellationToken cancellationToken)
    {
        if (!AssessmentKindLabels.TryParse(request.TestKey, out var kind))
        {
            return BadRequest(new { message = "Onbekende test." });
        }

        await _research.RecordPilotAttemptAsync(
            sessionId,
            kind,
            request.TestVersion,
            request.Answers ?? new Dictionary<int, int>(),
            request.ItemDurationMs,
            request.ItemUnclear,
            request.TotalDurationMs,
            request.EndComment,
            cancellationToken);
        return NoContent();
    }
}
