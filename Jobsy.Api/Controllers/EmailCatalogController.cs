using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/settings/email-templates")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
[EnableRateLimiting("public-write")]
public sealed class EmailCatalogController : ControllerBase
{
    private readonly IEmailCatalogService _catalog;
    private readonly IPlatformFeatureService _features;

    public EmailCatalogController(IEmailCatalogService catalog, IPlatformFeatureService features)
    {
        _catalog = catalog;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmailTemplateListItem>>> List(CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        return Ok(_catalog.ListTemplates(features.AmbassadorsEnabled));
    }

    [HttpGet("options")]
    public ActionResult<EmailCatalogTestOptions> Options()
        => Ok(_catalog.GetTestOptions());

    [HttpGet("{key}/preview")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<EmailTemplatePreview>> Preview(
        string key,
        [FromQuery] string lang = "nl",
        [FromQuery] string theme = "light",
        CancellationToken cancellationToken = default)
    {
        var features = await _features.GetAsync(cancellationToken);
        try
        {
            var preview = _catalog.Preview(key, lang, theme, features.PublicWebBaseUrl);
            Response.Headers.CacheControl = "no-store";
            return Ok(preview);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Onbekend mailtype." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{key}/send")]
    public async Task<ActionResult<EmailCatalogSendResult>> Send(
        string key,
        [FromBody] EmailCatalogSendRequest? request,
        CancellationToken cancellationToken)
    {
        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var result = await _catalog.SendAsync(
            key,
            request?.Lang ?? "nl",
            adminEmail,
            request?.To,
            cancellationToken);
        if (!result.Ok && result.Message.Contains("allow-list", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = result.Message });
        }

        if (!result.Ok && result.Message.Contains("Daglimiet", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = result.Message });
        }

        if (!result.Ok && result.Message.Contains("Onbekend", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = result.Message });
        }

        if (!result.Ok)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    [HttpPost("send-all")]
    public async Task<ActionResult<EmailCatalogSendAllAccepted>> SendAll(
        [FromBody] EmailCatalogSendRequest? request,
        CancellationToken cancellationToken)
    {
        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = Guid.TryParse(adminIdClaim, out var adminId);
        try
        {
            var accepted = await _catalog.StartSendAllAsync(
                request?.Lang ?? "nl",
                adminEmail,
                request?.To,
                adminId,
                cancellationToken);
            return Accepted(accepted);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("15 minuten", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("send-all/{runId:guid}")]
    public ActionResult<EmailCatalogSendAllStatus> SendAllStatus(Guid runId)
    {
        var status = _catalog.GetSendAllStatus(runId);
        return status is null ? NotFound() : Ok(status);
    }
}

public sealed record EmailCatalogSendRequest(string? To, string? Lang = null, string? Theme = null);
