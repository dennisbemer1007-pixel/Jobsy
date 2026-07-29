using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

/// <summary>Employer self-service for company API keys (bedrijfsmanager).</summary>
[ApiController]
[Route("api/companies/{companyId:guid}/api-keys")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
public class CompanyApiKeysController : ControllerBase
{
    private readonly ICompanyApiKeyService _apiKeys;
    private readonly IUserLookupService _users;
    private readonly JobsyDbContext _db;

    public CompanyApiKeysController(
        ICompanyApiKeyService apiKeys,
        IUserLookupService users,
        JobsyDbContext db)
    {
        _apiKeys = apiKeys;
        _users = users;
        _db = db;
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

    [HttpPost("email-credentials")]
    [RequireCompanyAccess]
    public async Task<ActionResult<EmailApiKeyResult>> EmailCredentials(
        Guid companyId,
        [FromBody] EmailApiKeyRequest? request,
        CancellationToken cancellationToken)
    {
        var recipient = await ResolveRecipientEmailAsync(companyId, request?.Email, cancellationToken);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return BadRequest(new
            {
                message = "Geen e-mailadres beschikbaar. Koppel een bedrijfsmanager of geef een e-mailadres op."
            });
        }

        var result = await _apiKeys.EmailCredentialsAsync(companyId, recipient, cancellationToken);
        return Ok(result);
    }

    private async Task<string?> ResolveRecipientEmailAsync(
        Guid companyId,
        string? overrideEmail,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(overrideEmail))
        {
            return overrideEmail.Trim().ToLowerInvariant();
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (!string.IsNullOrWhiteSpace(actor?.Email))
        {
            return actor.Email.Trim().ToLowerInvariant();
        }

        // Fall back to an active enterprise manager for this company.
        return await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.EnterpriseManager)
            .Where(u => u.CompanyId == companyId
                        || u.CompanyMemberships.Any(m => m.CompanyId == companyId))
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
