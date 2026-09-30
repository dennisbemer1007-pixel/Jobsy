using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>Public one-time objection for "Salesmanager aanbevelen" notice mail (D15).</summary>
[ApiController]
[Route("api/sales/recommend")]
[AllowAnonymous]
public sealed class SalesRecommendPublicController : ControllerBase
{
    private readonly ISalesManagerApplicationService _applications;

    public SalesRecommendPublicController(ISalesManagerApplicationService applications)
        => _applications = applications;

    [HttpPost("object")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Object(
        [FromBody] SalesRecommendObjectRequest? request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
        {
            return BadRequest(new { message = "Ongeldige link." });
        }

        var ok = await _applications.ObjectByTokenAsync(request.Token, cancellationToken);
        if (!ok)
        {
            return NotFound(new { message = "Deze link is al gebruikt of niet meer geldig." });
        }

        return Ok(new { message = "Je gegevens zijn verwijderd." });
    }
}

public sealed record SalesRecommendObjectRequest(string? Token);
