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
    public ActionResult<UnsubscribePreviewResponse> Preview([FromQuery] string? token)
    {
        if (!_tokens.TryValidate(token, out _, out var category, out _)
            || !EmailOptionalCategories.IsOptional(category))
        {
            return Ok(new UnsubscribePreviewResponse(Valid: false));
        }

        return Ok(new UnsubscribePreviewResponse(Valid: true, Category: category));
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
        if (!_tokens.TryValidate(request.Token, out var emailHash, out var category, out _))
        {
            return BadRequest(new UnsubscribeResponse(false, Message: "invalid_or_expired"));
        }

        if (!EmailOptionalCategories.IsOptional(category))
        {
            return BadRequest(new UnsubscribeResponse(false, Message: "invalid_or_expired"));
        }

        var optIn = string.Equals(request.Action, "opt-in", StringComparison.OrdinalIgnoreCase);
        if (optIn)
        {
            await _preferences.OptInByHashAsync(emailHash, category, cancellationToken);
        }
        else
        {
            await _preferences.OptOutByHashAsync(emailHash, category, "OneClick", cancellationToken);
        }

        return Ok(new UnsubscribeResponse(true, category));
    }
}
