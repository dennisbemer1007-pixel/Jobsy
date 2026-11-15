using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Infrastructure.WerkgeverApk;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/werkgever-apk/employee")]
[WerkgeverApkFeatureGate]
[EnableRateLimiting("pupil")]
public sealed class WerkgeverApkEmployeeController : ControllerBase
{
    private readonly IWerkgeverApkEmployeePortalService _portal;

    public WerkgeverApkEmployeeController(IWerkgeverApkEmployeePortalService portal) => _portal = portal;

    [HttpGet("login-options")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginOptions(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await _portal.ListLoginCompaniesAsync(ct));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] WerkgeverApkEmployeeLoginRequest request, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.LoginAsync(request, ct);
        if (ok is null)
        {
            return StatusCode(status, error);
        }

        var principal = _portal.CreatePrincipal(ok);
        await HttpContext.SignInAsync(
            WerkgeverApkAuthDefaults.Scheme,
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
    [Authorize(Policy = JobsyPolicies.WerkgeverApkEmployeeSession)]
    public async Task<IActionResult> Progress(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.GetProgressAsync(User, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPost("welcome")]
    [Authorize(Policy = JobsyPolicies.WerkgeverApkEmployeeSession)]
    public async Task<IActionResult> Welcome(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.MarkWelcomeSeenAsync(User, ct);
        return ok ? Ok() : StatusCode(status, error);
    }

    [HttpGet("blocks/{blockKey}/question")]
    [Authorize(Policy = JobsyPolicies.WerkgeverApkEmployeeSession)]
    public async Task<IActionResult> Question(string blockKey, [FromQuery] string? itemId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!PupilMboHboEntryPaths.TryParseBlockRoute(blockKey, out var block))
        {
            return BadRequest(new WerkgeverApkErrorDto("invalid_block", "Onbekende test."));
        }

        var (ok, error, status) = await _portal.GetQuestionAsync(User, block, itemId, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("blocks/{blockKey}/answers/{itemId}")]
    [Authorize(Policy = JobsyPolicies.WerkgeverApkEmployeeSession)]
    public async Task<IActionResult> Answer(
        string blockKey,
        string itemId,
        [FromBody] WerkgeverApkEmployeeAnswerRequest request,
        CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!PupilMboHboEntryPaths.TryParseBlockRoute(blockKey, out var block))
        {
            return BadRequest(new WerkgeverApkErrorDto("invalid_block", "Onbekende test."));
        }

        var (ok, error, status) = await _portal.SaveAnswerAsync(User, block, itemId, request.Value, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("share-preview")]
    [Authorize(Policy = JobsyPolicies.WerkgeverApkEmployeeSession)]
    public async Task<IActionResult> SharePreview(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.GetSharePreviewAsync(User, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPost("share")]
    [Authorize(Policy = JobsyPolicies.WerkgeverApkEmployeeSession)]
    public async Task<IActionResult> Share([FromBody] WerkgeverApkShareWithManagerRequest request, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.SetShareAsync(User, request.Share, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("comparison")]
    [Authorize(Policy = JobsyPolicies.WerkgeverApkEmployeeSession)]
    public async Task<IActionResult> Comparison(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var (ok, error, status) = await _portal.GetComparisonAsync(User, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }
}