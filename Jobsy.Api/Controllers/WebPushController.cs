using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/push")]
public sealed class WebPushController : ControllerBase
{
    private readonly IWebPushSubscriptionService _subscriptions;
    private readonly IUserLookupService _users;
    private readonly WebPushVapidKeyProvider _vapid;

    public WebPushController(
        IWebPushSubscriptionService subscriptions,
        IUserLookupService users,
        WebPushVapidKeyProvider vapid)
    {
        _subscriptions = subscriptions;
        _users = users;
        _vapid = vapid;
    }

    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public ActionResult<WebPushVapidPublicKeyDto> VapidPublicKey()
        => Ok(new WebPushVapidPublicKeyDto(_vapid.PublicKey));

    [HttpPost("subscribe")]
    [Authorize]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Subscribe(
        [FromBody] WebPushSubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (request?.Keys is null
            || string.IsNullOrWhiteSpace(request.Endpoint)
            || string.IsNullOrWhiteSpace(request.Keys.P256dh)
            || string.IsNullOrWhiteSpace(request.Keys.Auth))
        {
            return BadRequest(new { error = "Incomplete subscription." });
        }

        await _subscriptions.UpsertAsync(
            user.Id,
            new WebPushSubscriptionInput(
                request.Endpoint,
                request.Keys.P256dh,
                request.Keys.Auth,
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("unsubscribe")]
    [Authorize]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] WebPushUnsubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request?.Endpoint))
        {
            await _subscriptions.RemoveAllAsync(user.Id, cancellationToken);
        }
        else
        {
            await _subscriptions.RemoveAsync(user.Id, request.Endpoint, cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("subscriptions")]
    [Authorize]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<WebPushSubscriptionDto>>> List(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var rows = await _subscriptions.ListForUserAsync(user.Id, cancellationToken);
        return Ok(rows.Select(r => new WebPushSubscriptionDto(r.Id, MaskEndpoint(r.Endpoint), r.CreatedAtUtc, r.LastUsedAtUtc)));
    }

    private static string MaskEndpoint(string endpoint)
    {
        if (endpoint.Length <= 48)
        {
            return endpoint;
        }

        return endpoint[..24] + "…" + endpoint[^12..];
    }
}
