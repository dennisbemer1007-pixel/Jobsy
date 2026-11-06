using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/proactive-matching")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
[RequiresFeature(PlatformFeature.ProactiveMatching)]
public sealed class ProactiveMatchingController : ControllerBase
{
    private readonly IProactiveMatchingService _matching;
    private readonly IUserLookupService _users;

    public ProactiveMatchingController(IProactiveMatchingService matching, IUserLookupService users)
    {
        _matching = matching;
        _users = users;
    }

    [HttpGet("settings")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<ProactiveMatchingSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var settings = await _matching.GetSettingsAsync(user.Id, cancellationToken);
        if (settings is null)
        {
            return NotFound();
        }

        return Ok(settings);
    }

    [HttpPut("settings")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<ProactiveMatchingSettingsDto>> UpdateSettings(
        [FromBody] ProactiveMatchingSettingsUpdate body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            return Ok(await _matching.UpsertSettingsAsync(user.Id, body, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("suggestions")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<ProactiveMatchSuggestionDto>>> ListSuggestions(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _matching.ListSuggestionsAsync(user.Id, take, cancellationToken));
    }

    [HttpPost("suggestions/{id:guid}/feedback")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<ProactiveMatchSuggestionDto>> Feedback(
        Guid id,
        [FromBody] ProactiveMatchFeedbackRequest body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var dto = await _matching.SubmitFeedbackAsync(user.Id, id, body.Feedback, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record ProactiveMatchFeedbackRequest(ProactiveMatchFeedback Feedback);
