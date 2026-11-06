using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Reminders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/reminder-whatsapp")]
[Authorize]
public sealed class MeReminderWhatsAppController : ControllerBase
{
    private readonly IUserLookupService _users;
    private readonly ComebackReminderService _reminders;

    public MeReminderWhatsAppController(IUserLookupService users, ComebackReminderService reminders)
    {
        _users = users;
        _reminders = reminders;
    }

    public sealed record WhatsAppReminderDto(bool Available, bool OptedIn, string? Phone);

    public sealed record UpdateWhatsAppReminderRequest(bool OptedIn, string? Phone);

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<WhatsAppReminderDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var view = await _reminders.GetWhatsAppAsync(user.Id, cancellationToken);
        return Ok(new WhatsAppReminderDto(view.Available, view.OptedIn, view.Phone));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<WhatsAppReminderDto>> Put(
        [FromBody] UpdateWhatsAppReminderRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var (ok, error, view) = await _reminders.SetWhatsAppAsync(
            user.Id, request.OptedIn, request.Phone, cancellationToken);
        if (!ok && error == "unavailable")
        {
            return NotFound(new { message = "WhatsApp-herinneringen staan uit." });
        }

        if (!ok)
        {
            return BadRequest(new { message = "Vul een geldig telefoonnummer in." });
        }

        return Ok(new WhatsAppReminderDto(view.Available, view.OptedIn, view.Phone));
    }
}
