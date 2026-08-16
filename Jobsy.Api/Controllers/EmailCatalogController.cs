using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
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

    public EmailCatalogController(IEmailCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<EmailTemplateInfo>> List()
        => Ok(_catalog.ListTemplates());

    [HttpPost("{key}/send")]
    public async Task<ActionResult<EmailCatalogSendResult>> Send(
        string key,
        [FromBody] EmailCatalogSendRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _catalog.SendAsync(key, request?.To ?? string.Empty, cancellationToken);
        if (!result.Ok && result.Message.Contains("geldig", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = result.Message });
        }

        if (!result.Ok && result.Message.Contains("Onbekend", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(result);
    }

    [HttpPost("send-all")]
    public async Task<ActionResult<IReadOnlyList<EmailCatalogSendResult>>> SendAll(
        [FromBody] EmailCatalogSendRequest? request,
        CancellationToken cancellationToken)
    {
        var results = await _catalog.SendAllAsync(request?.To ?? string.Empty, cancellationToken);
        if (results.Count == 1 && !results[0].Ok && results[0].Message.Contains("geldig", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = results[0].Message });
        }

        return Ok(results);
    }
}

public sealed record EmailCatalogSendRequest(string? To);
