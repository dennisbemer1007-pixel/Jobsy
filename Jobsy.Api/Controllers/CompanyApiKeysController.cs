using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>Employer self-service for company API keys (bedrijfsmanager).</summary>
[ApiController]
[Route("api/companies/{companyId:guid}/api-keys")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
[EnableRateLimiting("auth")]
public class CompanyApiKeysController : ControllerBase
{
    private readonly ICompanyApiKeyService _apiKeys;
    private readonly IUserLookupService _users;

    public CompanyApiKeysController(
        ICompanyApiKeyService apiKeys,
        IUserLookupService users)
    {
        _apiKeys = apiKeys;
        _users = users;
    }

    [HttpGet]
    [RequireCompanyAccess]
    public async Task<ActionResult<IEnumerable<CompanyApiKeyView>>> List(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var items = await _apiKeys.ListForCompanyAsync(companyId, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [RequireCompanyAccess]
    public async Task<ActionResult<GeneratedApiKeyResponse>> Generate(
        Guid companyId,
        [FromBody] GenerateApiKeyRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _apiKeys.GenerateAsync(companyId, request?.Name, cancellationToken);
            return Ok(new GeneratedApiKeyResponse(
                result.Id,
                result.CompanyId,
                result.Name,
                result.KeyPrefix,
                result.PlaintextKey,
                result.CreatedAt,
                "Bewaar deze API-key nu — hij wordt hierna niet opnieuw getoond."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{apiKeyId:guid}/deactivate")]
    [RequireCompanyAccess]
    public async Task<IActionResult> Deactivate(
        Guid companyId,
        Guid apiKeyId,
        CancellationToken cancellationToken)
    {
        var key = (await _apiKeys.ListForCompanyAsync(companyId, cancellationToken))
            .FirstOrDefault(k => k.Id == apiKeyId);
        if (key is null)
        {
            return NotFound(new { message = "API-key niet gevonden." });
        }

        await _apiKeys.DeactivateAsync(apiKeyId, cancellationToken);
        return Ok(new { message = "API-key gedeactiveerd." });
    }

    [HttpPost("deactivate-active")]
    [RequireCompanyAccess]
    public async Task<IActionResult> DeactivateActive(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var deactivated = await _apiKeys.DeactivateForCompanyAsync(companyId, cancellationToken);
        if (!deactivated)
        {
            return NotFound(new { message = "Geen actieve API-key gevonden." });
        }

        return Ok(new { message = "Actieve API-key gedeactiveerd." });
    }

    /// <summary>
    /// Rotates to a new API key and e-mails it to the signed-in bedrijfsmanager.
    /// Arbitrary recipient addresses are rejected to prevent credential exfiltration.
    /// </summary>
    [HttpPost("email-credentials")]
    [RequireCompanyAccess]
    public async Task<ActionResult<EmailApiKeyResult>> EmailCredentials(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var recipient = actor?.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return BadRequest(new
            {
                message = "Geen e-mailadres op jouw account. Log opnieuw in of werk je profiel bij."
            });
        }

        try
        {
            var result = await _apiKeys.EmailCredentialsAsync(companyId, recipient, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
