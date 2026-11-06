using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/email-preferences")]
public sealed class EmailPreferencesUnsubscribeController : ControllerBase
{
    private readonly IMailUnsubscribeTokenService _tokens;
    private readonly IEmailPreferenceService _preferences;

    public EmailPreferencesUnsubscribeController(
        IMailUnsubscribeTokenService tokens,
        IEmailPreferenceService preferences)
    {
        _tokens = tokens;
        _preferences = preferences;
    }

    public sealed record UnsubscribeRequest(string? Token, string? Action = null);

    public sealed record UnsubscribeResponse(bool Ok, string? Category = null, string? Message = null);

    public sealed record UnsubscribePreviewResponse(bool Valid, string? Category = null);

    [AllowAnonymous]
    [HttpGet("unsubscribe/preview")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<UnsubscribePreviewResponse>> Preview(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        if (!await TokenAllowsAsync(token, cancellationToken))
        {
            return Ok(new UnsubscribePreviewResponse(Valid: false));
        }

        _tokens.TryRead(token, out var claims);
        return Ok(new UnsubscribePreviewResponse(Valid: true, Category: claims!.Category));
    }

    /// <summary>
    /// Validates the signed token and writes an opt-out (or opt-in when action=opt-in).
    /// Never reveals whether the address exists.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("unsubscribe")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<UnsubscribeResponse>> Unsubscribe(
        [FromBody] UnsubscribeRequest request,
        CancellationToken cancellationToken)
    {
        if (!await TokenAllowsAsync(request.Token, cancellationToken)
            || !_tokens.TryRead(request.Token, out var claims)
            || claims is null)
        {
            return BadRequest(new UnsubscribeResponse(false, Message: "invalid_or_expired"));
        }

        var optIn = string.Equals(request.Action, "opt-in", StringComparison.OrdinalIgnoreCase);
        if (optIn)
        {
            if (claims.UserId is Guid userId)
            {
                await _preferences.RestoreOptionalCategoryAsync(userId, claims.Category, "OneClick", cancellationToken);
            }
            else
            {
                await _preferences.OptInByHashAsync(claims.EmailHash, claims.Category, cancellationToken);
            }
        }
        else if (claims.UserId is Guid optOutUserId)
        {
            await _preferences.DisableReminderEmailsAsync(optOutUserId, "OneClick", cancellationToken);
            await _preferences.OptOutByHashAsync(claims.EmailHash, claims.Category, "OneClick", cancellationToken);
        }
        else
        {
            await _preferences.OptOutByHashAsync(claims.EmailHash, claims.Category, "OneClick", cancellationToken);
        }

        return Ok(new UnsubscribeResponse(true, claims.Category));
    }

    private async Task<bool> TokenAllowsAsync(string? token, CancellationToken cancellationToken)
    {
        if (!_tokens.TryRead(token, out var claims)
            || claims is null
            || !EmailOptionalCategories.IsOptional(claims.Category))
        {
            return false;
        }

        if (claims.UserId is Guid userId
            && !await _preferences.UnsubscribeEpochAllowsAsync(userId, claims.Epoch, cancellationToken))
        {
            return false;
        }

        return true;
    }
}
