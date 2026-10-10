using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/public/external-vacancy")]
[AllowAnonymous]
public sealed class ExternalVacancyPublicController : ControllerBase
{
    private readonly IExternalVacancyEmployerInviteService _invites;
    private readonly IExternalVacancySuppressionService _suppression;

    public ExternalVacancyPublicController(
        IExternalVacancyEmployerInviteService invites,
        IExternalVacancySuppressionService suppression)
    {
        _invites = invites;
        _suppression = suppression;
    }

    [HttpGet("employer-invite")]
    public async Task<IActionResult> ResolveEmployerInvite(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var dto = await _invites.ResolveInviteAsync(token, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("unsubscribe")]
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (!_suppression.TryValidateUnsubscribeToken(token, out var email))
        {
            return BadRequest(new { message = "Ongeldige link." });
        }

        await _suppression.SuppressAsync(email, "unsubscribe_link", cancellationToken);
        return Ok(new { message = "Je ontvangt geen externe sollicitaties meer op dit adres." });
    }
}
