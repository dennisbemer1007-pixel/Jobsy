using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/pupil/mbo-hbo")]
[SchoolsFeatureGate]
[EnableRateLimiting("pupil")]
public sealed class PupilMboHboController : ControllerBase
{
    private readonly IPupilMboHboPortalService _portal;

    public PupilMboHboController(IPupilMboHboPortalService portal) => _portal = portal;

    [HttpGet("overview")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.GetOverviewAsync(User, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPost("intro")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Intro(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.MarkIntroSeenAsync(User, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("blocks/{blockKey}/question")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Question(string blockKey, [FromQuery] string? itemId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!PupilMboHboEntryPaths.TryParseBlockRoute(blockKey, out var block))
        {
            return BadRequest(new PupilErrorDto("invalid_block", "Onbekende test."));
        }

        var (ok, error, status) = await _portal.GetQuestionAsync(User, block, itemId, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("blocks/{blockKey}/answers/{itemId}")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Answer(
        string blockKey,
        string itemId,
        [FromBody] PupilMboHboAnswerRequest request,
        CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!PupilMboHboEntryPaths.TryParseBlockRoute(blockKey, out var block))
        {
            return BadRequest(new PupilErrorDto("invalid_block", "Onbekende test."));
        }

        if (block == PupilMboHboBlock.Vakken)
        {
            return BadRequest(new PupilErrorDto("invalid_block", "Gebruik vak-antwoord endpoint."));
        }

        var (ok, error, status) = await _portal.SaveAnswerAsync(User, block, itemId, request.Value, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("blocks/vakken/answers/{itemId}")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> VakAnswer(
        string itemId,
        [FromBody] PupilMboHboVakAnswerRequest request,
        CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.SaveVakAnswerAsync(
            User,
            itemId,
            request.Leuk,
            request.Goed,
            ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("blocks/{blockKey}/outcome")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Outcome(string blockKey, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!PupilMboHboEntryPaths.TryParseBlockRoute(blockKey, out var block))
        {
            return BadRequest(new PupilErrorDto("invalid_block", "Onbekende test."));
        }

        var (ok, error, status) = await _portal.GetOutcomeAsync(User, block, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("passport")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Passport(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.GetPassportAsync(User, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }
}
