using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services.CandidateExternalVacancies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/public/external-vacancy")]
[AllowAnonymous]
public sealed class ExternalVacancyPublicController : ControllerBase
{
    private static readonly byte[] TransparentGif =
    [
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00,
        0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3B
    ];

    private readonly IExternalVacancyEmployerInviteService _invites;
    private readonly IExternalVacancySuppressionService _suppression;
    private readonly IExternalVacancyOutboundMetricsService _metrics;

    public ExternalVacancyPublicController(
        IExternalVacancyEmployerInviteService invites,
        IExternalVacancySuppressionService suppression,
        IExternalVacancyOutboundMetricsService metrics)
    {
        _invites = invites;
        _suppression = suppression;
        _metrics = metrics;
    }

    [HttpGet("open")]
    public async Task<IActionResult> MarkOpened([FromQuery] string token, CancellationToken cancellationToken)
    {
        await _metrics.MarkEmailOpenedAsync(token, cancellationToken);
        return File(TransparentGif, "image/gif");
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
