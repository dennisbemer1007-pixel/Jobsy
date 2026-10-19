using System.Security.Claims;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/contextual-tips")]
[Authorize(Roles = "Candidate")]
[EnableRateLimiting("authenticated")]
public sealed class ContextualTipsController : ControllerBase
{
    private readonly IContextualTipsService _tips;

    public ContextualTipsController(IContextualTipsService tips) => _tips = tips;

    [HttpGet]
    public async Task<ActionResult<ContextualTipsStateDto>> Get(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _tips.GetStateAsync(userId.Value, cancellationToken));
    }

    [HttpPost("hide")]
    public async Task<ActionResult<ContextualTipsStateDto>> Hide(
        [FromBody] HideContextualTipRequest body,
        CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _tips.HideTipAsync(userId.Value, body.TipId, cancellationToken);
        return Ok(await _tips.GetStateAsync(userId.Value, cancellationToken));
    }

    [HttpPost("restore-hidden")]
    public async Task<ActionResult<ContextualTipsStateDto>> RestoreHidden(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _tips.RestoreHiddenAsync(userId.Value, cancellationToken);
        return Ok(await _tips.GetStateAsync(userId.Value, cancellationToken));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ContextualTipsStateDto>> UpdateSettings(
        [FromBody] UpdateContextualTipsSettingsRequest body,
        CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _tips.UpdateSettingsAsync(userId.Value, body, cancellationToken));
    }

    private Guid? UserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record HideContextualTipRequest(string TipId);
