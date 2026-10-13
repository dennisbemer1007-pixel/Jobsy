using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/company-api-keys")]
public sealed class CompanyApiKeyRevealController : ControllerBase
{
    private readonly ICompanyApiKeyService _apiKeys;
    private readonly IOneTimeLinkService _links;

    public CompanyApiKeyRevealController(ICompanyApiKeyService apiKeys, IOneTimeLinkService links)
    {
        _apiKeys = apiKeys;
        _links = links;
    }

    public sealed record RevealRequest(string? Token);

    [AllowAnonymous]
    [HttpGet("reveal-preview")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> Preview(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        var peek = await _links.PeekAsync(OneTimeLinkPurpose.ApiKeyReveal, token ?? "", cancellationToken);
        Response.Headers.CacheControl = "no-store";
        if (!peek.Valid)
        {
            return Ok(new { valid = false });
        }

        return Ok(new
        {
            valid = true,
            companyName = peek.CompanyName,
            expiresAtUtc = peek.ExpiresAtUtc
        });
    }

    [AllowAnonymous]
    [HttpPost("reveal")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> Reveal(
        [FromBody] RevealRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _apiKeys.RevealFromTokenAsync(request.Token ?? "", cancellationToken);
        if (result is null)
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            ok = true,
            plaintextKey = result.PlaintextKey,
            keyPrefix = result.KeyPrefix,
            companyName = result.CompanyName,
            apiBaseUrl = result.ApiBaseUrl,
            endpoint = result.ApiBaseUrl.TrimEnd('/') + "/api/external/vacancies",
            headerName = "X-API-Key",
            swaggerUrl = result.ApiBaseUrl.TrimEnd('/') + "/swagger"
        });
    }
}
