using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/auth/device-sessions")]
public sealed class DeviceSessionsController : ControllerBase
{
    private readonly IDeviceSessionService _sessions;
    private readonly IUserLookupService _users;
    private readonly JobsyDbContext _db;

    public DeviceSessionsController(
        IDeviceSessionService sessions,
        IUserLookupService users,
        JobsyDbContext db)
    {
        _sessions = sessions;
        _users = users;
        _db = db;
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<DeviceSessionCreatedDto>> Create(
        [FromBody] CreateDeviceSessionRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var created = await _sessions.CreateAsync(
            user.Id,
            request?.UserAgent ?? Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(new DeviceSessionCreatedDto(
            created.DeviceSessionId,
            created.RefreshToken,
            created.ExpiresAtUtc,
            created.DeviceName,
            user.SessionVersion));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<DeviceSessionRefreshResponse>> Refresh(
        [FromBody] DeviceSessionRefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            return Unauthorized();
        }

        var rotated = await _sessions.RotateAsync(
            request.RefreshToken,
            request.UserAgent ?? Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (rotated is null)
        {
            return Unauthorized();
        }

        return Ok(new DeviceSessionRefreshResponse(
            rotated.RefreshToken,
            rotated.DeviceSessionId,
            rotated.ExpiresAtUtc,
            rotated.Email,
            rotated.FullName,
            rotated.Role,
            rotated.CompanyId,
            rotated.CompanyIds,
            rotated.ShowCandidateHowTo,
            rotated.HasCandidateApplications,
            rotated.HasSalesReferral,
            rotated.SessionVersion,
            rotated.SessionToken));
    }

    [HttpGet]
    [Authorize]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<DeviceSessionListItemDto>>> List(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        Guid? currentId = null;
        if (Guid.TryParse(User.FindFirst(JobsyClaimTypes.DeviceSessionId)?.Value, out var parsed))
        {
            currentId = parsed;
        }

        var rows = await _sessions.ListAsync(user.Id, cancellationToken);
        return Ok(rows.Select(r => new DeviceSessionListItemDto(
            r.Id,
            r.DeviceName,
            r.LastUsedAtUtc,
            r.CreatedAtUtc,
            r.ExpiresAtUtc,
            currentId == r.Id)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var ok = await _sessions.RevokeAsync(
            user.Id,
            id,
            DeviceSessionRules.RevokeReasonLogout,
            cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await _sessions.RevokeAllAsync(
            user.Id,
            DeviceSessionRules.RevokeReasonLogoutAll,
            bumpSessionVersion: true,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("validity")]
    [Authorize]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<SessionValidityDto>> Validity(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var snap = await _sessions.GetSessionValidityAsync(user.Id, cancellationToken);
        return Ok(new SessionValidityDto(snap.SessionVersion, snap.MinimumSessionVersion));
    }

    [HttpPost("handoff")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<DeviceHandoffCreatedDto>> CreateHandoff(
        [FromBody] CreateDeviceHandoffRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var created = await _sessions.CreateHandoffAsync(
            user.Id,
            request?.RememberDevice ?? true,
            request?.ReturnUrl,
            request?.UserAgent ?? Request.Headers.UserAgent.ToString(),
            cancellationToken);
        return Ok(new DeviceHandoffCreatedDto(created.Code, created.ExpiresAtUtc));
    }

    /// <summary>
    /// Server-to-server handoff after external login (provision secret), so the Web
    /// OIDC callback can mint a code before the PWA exchanges it in-scope.
    /// </summary>
    [HttpPost("handoff/provision")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<DeviceHandoffCreatedDto>> ProvisionHandoff(
        [FromBody] ProvisionDeviceHandoffRequest request,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var expected = configuration["JobsyAuth:ExternalProvisionSecret"];
        var provided = Request.Headers["X-Jobsy-Provision-Secret"].ToString();
        if (string.IsNullOrWhiteSpace(expected)
            || !string.Equals(expected, provided, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var created = await _sessions.CreateHandoffAsync(
            user.Id,
            request.RememberDevice,
            request.ReturnUrl,
            request.UserAgent,
            cancellationToken);
        return Ok(new DeviceHandoffCreatedDto(created.Code, created.ExpiresAtUtc));
    }

    /// <summary>
    /// Server-to-server: create a device session after Web demo/password login when the
    /// API local-login path was not used (e.g. DemoUserStore).
    /// </summary>
    [HttpPost("for-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<DeviceSessionCreatedDto>> CreateForLogin(
        [FromBody] ProvisionDeviceHandoffRequest request,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var expected = configuration["JobsyAuth:ExternalProvisionSecret"];
        var provided = Request.Headers["X-Jobsy-Provision-Secret"].ToString();
        if (string.IsNullOrWhiteSpace(expected)
            || !string.Equals(expected, provided, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.RememberDevice)
        {
            return BadRequest();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var created = await _sessions.CreateAsync(
            user.Id,
            request.UserAgent ?? Request.Headers.UserAgent.ToString(),
            cancellationToken);
        return Ok(new DeviceSessionCreatedDto(
            created.DeviceSessionId,
            created.RefreshToken,
            created.ExpiresAtUtc,
            created.DeviceName,
            user.SessionVersion));
    }

    [HttpPost("handoff/exchange")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<DeviceHandoffExchangeResponse>> ExchangeHandoff(
        [FromBody] ExchangeDeviceHandoffRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return Unauthorized();
        }

        var result = await _sessions.ExchangeHandoffAsync(
            request.Code,
            request.UserAgent ?? Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(new DeviceHandoffExchangeResponse(
            result.Email,
            result.FullName,
            result.Role,
            result.CompanyId,
            result.CompanyIds,
            result.ShowCandidateHowTo,
            result.HasCandidateApplications,
            result.HasSalesReferral,
            result.SessionVersion,
            result.SessionToken,
            result.ReturnUrl,
            result.DeviceSession?.DeviceSessionId,
            result.DeviceSession?.RefreshToken,
            result.DeviceSession?.ExpiresAtUtc));
    }
}
