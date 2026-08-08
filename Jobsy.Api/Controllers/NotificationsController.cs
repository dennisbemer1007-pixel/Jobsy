using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IUserNotificationService _notifications;
    private readonly IUserLookupService _users;

    public NotificationsController(IUserNotificationService notifications, IUserLookupService users)
    {
        _notifications = notifications;
        _users = users;
    }

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<UserNotificationDto>>> List(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var rows = await _notifications.ListForUserAsync(user.Id, take, cancellationToken);
        return Ok(rows.Select(n => new UserNotificationDto(
            n.Id,
            n.Title,
            n.Body,
            n.Category,
            n.DeepLink,
            n.ActionLabel,
            Jobsy.Infrastructure.Services.UserNotificationService.SanitizeActionUrl(n.ActionUrl),
            n.IsRead,
            n.CreatedAtUtc,
            n.ReadAtUtc,
            n.RelatedEntityType,
            n.RelatedEntityId)));
    }

    [HttpGet("unread-count")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<UnreadNotificationCountDto>> UnreadCount(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var count = await _notifications.CountUnreadAsync(user.Id, cancellationToken);
        return Ok(new UnreadNotificationCountDto(count));
    }

    [HttpPost("{id:guid}/read")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<UserNotificationDto>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var row = await _notifications.MarkReadAsync(user.Id, id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return Ok(new UserNotificationDto(
            row.Id,
            row.Title,
            row.Body,
            row.Category,
            row.DeepLink,
            row.ActionLabel,
            row.ActionUrl,
            row.IsRead,
            row.CreatedAtUtc,
            row.ReadAtUtc,
            row.RelatedEntityType,
            row.RelatedEntityId));
    }

    [HttpPost("read-all")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<object>> MarkAllRead(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var updated = await _notifications.MarkAllReadAsync(user.Id, cancellationToken);
        return Ok(new { updated });
    }
}
