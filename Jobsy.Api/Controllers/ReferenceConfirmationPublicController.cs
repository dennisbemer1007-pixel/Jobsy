using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/public/reference-confirmations")]
[AllowAnonymous]
public class ReferenceConfirmationPublicController : ControllerBase
{
    private readonly ReferenceConfirmationService _confirmations;

    public ReferenceConfirmationPublicController(ReferenceConfirmationService confirmations)
    {
        _confirmations = confirmations;
    }

    [HttpGet("{token}")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult> Open(string token, CancellationToken cancellationToken)
    {
        var view = await _confirmations.OpenAsync(token, cancellationToken);
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
        [FromBody] SubmitReferenceConfirmationBody? body,
        CancellationToken cancellationToken)
    {
        var result = await _confirmations.SubmitAsync(
            token,
            body?.WorkedHere,
            body?.Period,
            body?.DidWell,
            body?.WorkAgain,
            body?.Extra,
            cancellationToken);
        return result == "ok" ? Ok(new { ok = true }) : BadRequest(new { error = result });
    }

    [HttpPost("{token}/decline")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Decline(string token, CancellationToken cancellationToken)
    {
        var result = await _confirmations.DeclineAsync(token, cancellationToken);
        return result == "ok" ? Ok(new { ok = true }) : BadRequest(new { error = result });
    }

    [HttpPost("{token}/misuse")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Misuse(
        string token,
        [FromBody] ReportReferenceMisuseBody? body,
        CancellationToken cancellationToken)
    {
        var result = await _confirmations.ReportMisuseAsync(token, body?.Message, cancellationToken);
        return result == "ok" ? Ok(new { ok = true }) : BadRequest(new { error = result });
    }
}

public sealed record SubmitReferenceConfirmationBody(
    bool? WorkedHere,
    string? Period,
    string? DidWell,
    string? WorkAgain,
    string? Extra);

public sealed record ReportReferenceMisuseBody(string? Message);
