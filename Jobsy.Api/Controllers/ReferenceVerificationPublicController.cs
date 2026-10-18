using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/public/reference-verification")]
[AllowAnonymous]
public class ReferenceVerificationPublicController : ControllerBase
{
    private readonly ReferenceVerificationService _verification;

    public ReferenceVerificationPublicController(ReferenceVerificationService verification)
    {
        _verification = verification;
    }

    [HttpGet("{token}")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult> Open(string token, CancellationToken cancellationToken)
    {
        var view = await _verification.OpenAsync(token, cancellationToken);
        if (view is null)
        {
            return NotFound(new { state = "invalid" });
        }

        return Ok(view);
    }

    [HttpPost("{token}/submit")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Submit(
        string token,
        [FromBody] SubmitWorkExperienceReferenceBody? body,
        CancellationToken cancellationToken)
    {
        var result = await _verification.SubmitAsync(
            token,
            body?.ConfirmedEmployer,
            body?.ConfirmedPeriod,
            body?.ConfirmedRole,
            body?.ConfirmedDuties,
            body?.EndorsedCompetenceCodes,
            body?.Comment,
            cancellationToken);
        return result == "ok" ? Ok(new { ok = true }) : BadRequest(new { error = result });
    }

    [HttpPost("{token}/decline")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Decline(string token, CancellationToken cancellationToken)
    {
        var result = await _verification.DeclineAsync(token, cancellationToken);
        return result == "ok" ? Ok(new { ok = true }) : BadRequest(new { error = result });
    }

    [HttpPost("{token}/unknown")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Unknown(string token, CancellationToken cancellationToken)
    {
        var result = await _verification.ReportUnknownAsync(token, cancellationToken);
        return result == "ok" ? Ok(new { ok = true }) : BadRequest(new { error = result });
    }
}

public sealed record SubmitWorkExperienceReferenceBody(
    bool? ConfirmedEmployer,
    bool? ConfirmedPeriod,
    bool? ConfirmedRole,
    bool? ConfirmedDuties,
    IReadOnlyList<string>? EndorsedCompetenceCodes,
    string? Comment);
