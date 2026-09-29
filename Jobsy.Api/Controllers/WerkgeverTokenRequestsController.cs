using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/werkgever/token-requests")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class WerkgeverTokenRequestsController : ControllerBase
{
    private readonly ITokenRequestService _requests;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;

    public WerkgeverTokenRequestsController(
        ITokenRequestService requests,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users)
    {
        _requests = requests;
        _companyAuth = companyAuth;
        _users = users;
    }

    public sealed record CreateTokenRequestBody(
        Guid BranchCompanyId,
        int Amount,
        string Reason,
        string? Note);

    public sealed record RejectTokenRequestBody(string? Reason);

    [HttpPost]
    [ProducesResponseType(typeof(TokenRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenRequestDto>> Create(
        [FromBody] CreateTokenRequestBody body,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole(JobsyRoles.RegionalManager)
            && !User.IsInRole(JobsyRoles.EnterpriseManager)
            && !User.IsInRole(JobsyRoles.Admin))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { code = "rm_forbidden", message = "Regiomanagers kunnen geen tokens aanvragen." });
        }

        if (User.IsInRole(JobsyRoles.EnterpriseManager)
            && !User.IsInRole(JobsyRoles.BranchManager)
            && !User.IsInRole(JobsyRoles.Admin))
        {
            return BadRequest(new { code = "bm_cannot_request", message = "Je kunt zelf tokens verdelen." });
        }

        if (!User.IsInRole(JobsyRoles.BranchManager) && !User.IsInRole(JobsyRoles.Admin))
        {
            return Forbid();
        }

        if (!await _companyAuth.CanAccessCompanyAsync(User, body.BranchCompanyId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { code = "branch_forbidden", message = "Geen toegang tot deze vestiging." });
        }

        if (!Enum.TryParse<TokenRequestReason>(body.Reason, ignoreCase: true, out var reason))
        {
            return BadRequest(new { code = "invalid_reason", message = "Ongeldige reden." });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        try
        {
            var dto = await _requests.CreateAsync(
                body.BranchCompanyId,
                actor.Id,
                body.Amount,
                reason,
                body.Note,
                cancellationToken);
            return Ok(dto);
        }
        catch (TokenRequestException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TokenRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TokenRequestDto>>> List(
        [FromQuery] string? status,
        [FromQuery] List<Guid>? companyIds,
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is null || accessible.Count == 0)
        {
            return Forbid();
        }

        IReadOnlyList<Guid> scope = accessible.ToList();
        if (companyIds is { Count: > 0 })
        {
            scope = companyIds.Where(accessible.Contains).Distinct().ToList();
            if (scope.Count == 0)
            {
                return Forbid();
            }
        }

        TokenRequestStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<TokenRequestStatus>(status, ignoreCase: true, out var s))
        {
            parsed = s;
        }

        var isBm = User.IsInRole(JobsyRoles.EnterpriseManager) || User.IsInRole(JobsyRoles.Admin);
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var list = await _requests.ListAsync(
            scope,
            forUserId: isBm ? null : actor?.Id,
            organisationWide: isBm,
            status: parsed,
            cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = JobsyRoles.TokenAllocateRoles)]
    [ProducesResponseType(typeof(TokenRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenRequestDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var existing = await _requests.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(new { code = "not_found", message = "Aanvraag niet gevonden." });
        }

        if (!await _companyAuth.CanAccessCompanyAsync(User, existing.OrganisationCompanyId, cancellationToken)
            && !_companyAuth.IsAdmin(User))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _requests.ApproveAsync(id, actor.Id, cancellationToken));
        }
        catch (TokenRequestException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = JobsyRoles.TokenAllocateRoles)]
    [ProducesResponseType(typeof(TokenRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenRequestDto>> Reject(
        Guid id,
        [FromBody] RejectTokenRequestBody? body,
        CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var existing = await _requests.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(new { code = "not_found", message = "Aanvraag niet gevonden." });
        }

        if (!await _companyAuth.CanAccessCompanyAsync(User, existing.OrganisationCompanyId, cancellationToken)
            && !_companyAuth.IsAdmin(User))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _requests.RejectAsync(id, actor.Id, body?.Reason, cancellationToken));
        }
        catch (TokenRequestException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/withdraw")]
    [Authorize(Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.Admin}")]
    [ProducesResponseType(typeof(TokenRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenRequestDto>> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        try
        {
            var dto = await _requests.WithdrawAsync(id, actor.Id, cancellationToken);
            return Ok(dto);
        }
        catch (TokenRequestException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }
}
