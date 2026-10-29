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
[Route("api/parent/passport")]
[SchoolsFeatureGate]
public sealed class ParentPassportController : ControllerBase
{
    private readonly IParentPassportService _parent;

    public ParentPassportController(IParentPassportService parent) => _parent = parent;

    [HttpPost("session")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> OpenSession(
        [FromBody] ParentPassportCodeRequest request,
        CancellationToken cancellationToken)
    {
        NoStore();
        var (ok, error, status) = await _parent.OpenSessionAsync(request.Code, cancellationToken);
        if (ok is null)
        {
            return StatusCode(status, error);
        }

        var principal = _parent.CreatePrincipal(ok);
        await HttpContext.SignInAsync(
            ParentPassportAuthDefaults.Scheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.FromUnixTimeSeconds(ok.AccessExpiresUnix)
            });

        return Ok(new { ok = true, pupilCodeId = ok.PupilCodeId, accessExpiresUnix = ok.AccessExpiresUnix });
    }

    [HttpGet("g78")]
    [Authorize(Policy = JobsyPolicies.ParentPassportSession)]
    public async Task<IActionResult> G78Passport(
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        NoStore();
        var (ok, error, status) = await _parent.GetG78PassportWebAsync(User, lang, cancellationToken);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("~/ouder/pdf")]
    [Authorize(Policy = JobsyPolicies.ParentPassportSession)]
    public async Task<IActionResult> Pdf(
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        NoStore();
        var (bytes, fileName, error, status) = await _parent.BuildPdfAsync(User, lang, cancellationToken);
        if (bytes is null)
        {
            return StatusCode(status, error);
        }

        return File(bytes, "application/pdf", fileName);
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";
}
