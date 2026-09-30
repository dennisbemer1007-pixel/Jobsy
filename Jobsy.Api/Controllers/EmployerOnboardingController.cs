using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer/onboarding")]
[Authorize(Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary}")]
public sealed class EmployerOnboardingController : ControllerBase
{
    private readonly IEmployerOnboardingStatusService _status;
    private readonly IVestigingSuggestionService _suggestions;
    private readonly IUserLookupService _users;

    public EmployerOnboardingController(
        IEmployerOnboardingStatusService status,
        IVestigingSuggestionService suggestions,
        IUserLookupService users)
    {
        _status = status;
        _suggestions = suggestions;
        _users = users;
    }

    [HttpGet("status")]
    public async Task<ActionResult<EmployerOnboardingStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _status.GetAsync(user.Id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpPost("vestiging-suggestions/dismiss")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<IActionResult> DismissSuggestion(
        [FromBody] VestigingSuggestionActionRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var status = await _status.GetAsync(user.Id, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        await _suggestions.DismissAsync(
            status.RootCompanyId,
            request.KvkEstablishmentId ?? "",
            user.Id,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("vestiging-suggestions/accept")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<IActionResult> AcceptSuggestion(
        [FromBody] VestigingSuggestionActionRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var status = await _status.GetAsync(user.Id, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        try
        {
            await _suggestions.AcceptAsync(
                status.RootCompanyId,
                request.KvkEstablishmentId ?? "",
                user.Id,
                cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record VestigingSuggestionActionRequest(string? KvkEstablishmentId);
