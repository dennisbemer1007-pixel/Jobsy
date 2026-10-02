using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/pupil")]
[SchoolsFeatureGate]
[EnableRateLimiting("pupil")]
public sealed class PupilController : ControllerBase
{
    private readonly IPupilPortalService _portal;

    public PupilController(IPupilPortalService portal) => _portal = portal;

    [HttpGet("schools")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PupilSchoolOptionDto>>> Schools(CancellationToken cancellationToken)
    {
        NoStore();
        return Ok(await _portal.ListSchoolsAsync(cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/classes")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PupilClassOptionDto>>> Classes(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        NoStore();
        var rows = await _portal.ListClassesAsync(schoolId, cancellationToken);
        return rows is null ? NotFound() : Ok(rows);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] PupilLoginRequest request,
        CancellationToken cancellationToken)
    {
        NoStore();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (ok, error, status) = await _portal.LoginAsync(request, ip, ua, cancellationToken);
        if (ok is null)
        {
            return StatusCode(status, error);
        }

        var principal = _portal.CreatePrincipal(ok);
        await HttpContext.SignInAsync(
            PupilAuthDefaults.Scheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc = null
            });

        return Ok(ok);
    }

    [HttpGet("progress")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Progress(CancellationToken cancellationToken)
    {
        NoStore();
        var (ok, error, status) = await _portal.GetProgressAsync(User, cancellationToken);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("progress/answers/{itemId}")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> SaveAnswer(
        string itemId,
        [FromBody] PupilAnswerRequest request,
        CancellationToken cancellationToken)
    {
        NoStore();
        var (ok, error, status) = await _portal.SaveAnswerAsync(User, itemId, request.Value, cancellationToken);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("progress/chips")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> SaveChips(
        [FromBody] PupilChipsRequest request,
        CancellationToken cancellationToken)
    {
        NoStore();
        var (ok, error, status) = await _portal.SaveChipsAsync(User, request, cancellationToken);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("result")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Result(CancellationToken cancellationToken)
    {
        NoStore();
        var (ok, error, status) = await _portal.GetResultAsync(User, cancellationToken);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("dreamjob")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> DreamJob(
        [FromBody] PupilDreamJobRequest request,
        CancellationToken cancellationToken)
    {
        NoStore();
        var (ok, error, status) = await _portal.SaveDreamJobAsync(User, request.Key, cancellationToken);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("~/leerling/pdf")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Pdf(CancellationToken cancellationToken)
    {
        NoStore();
        var (bytes, fileName, error, status) = await _portal.BuildPdfAsync(User, cancellationToken);
        if (bytes is null)
        {
            return StatusCode(status, error);
        }

        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost("logout")]
    [Authorize(Policy = JobsyPolicies.PupilSession)]
    public async Task<IActionResult> Logout()
    {
        NoStore();
        await HttpContext.SignOutAsync(PupilAuthDefaults.Scheme);
        return Ok(new { ok = true });
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";
}
