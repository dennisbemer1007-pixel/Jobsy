using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/access-requests")]
public sealed class AccessRequestsController : ControllerBase
{
    private readonly ICompanyAccessRequestService _access;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly ICompanyRegistrationService _registration;

    public AccessRequestsController(
        ICompanyAccessRequestService access,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        ICompanyRegistrationService registration)
    {
        _access = access;
        _companyAuth = companyAuth;
        _users = users;
        _registration = registration;
    }

    public sealed record SubmitBody(
        string KvkNumber,
        string? KvkEstablishmentId,
        Guid[]? RequestedCompanyIds,
        string RequestedRole,
        string RequesterName,
        string? RequesterFunction,
        string RequesterEmail,
        string? RequesterPhone,
        string? Message,
        Guid? TargetCompanyId = null);

    public sealed record ConfirmBody(string Code);

    public sealed record WithdrawBody(string Token);

    public sealed record DecideBody(
        string? GrantedRole,
        Guid[]? GrantedCompanyIds,
        string? Reason);

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("access-request")]
    public async Task<IActionResult> Submit([FromBody] SubmitBody body, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(body.RequestedRole, ignoreCase: true, out var role))
        {
            return BadRequest(new { message = "Ongeldige rol." });
        }

        try
        {
            var result = await _access.SubmitAsync(
                new AccessRequestSubmitRequest(
                    body.KvkNumber,
                    body.KvkEstablishmentId,
                    body.RequestedCompanyIds ?? [],
                    role,
                    body.RequesterName,
                    body.RequesterFunction,
                    body.RequesterEmail,
                    body.RequesterPhone,
                    body.Message,
                    body.TargetCompanyId),
                cancellationToken);

            // Never leak manager PII in anonymous responses.
            return Ok(new
            {
                requestId = result.RequestId,
                status = result.Status.ToString(),
                message = result.Message,
                codeExpiresAtUtc = result.CodeExpiresAtUtc
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmBody body, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _access.ConfirmEmailAsync(id, body.Code ?? "", cancellationToken);
            return Ok(new
            {
                requestId = result.RequestId,
                status = result.Status.ToString(),
                message = result.Message,
                companyName = result.CompanyName
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/withdraw")]
    [AllowAnonymous]
    [EnableRateLimiting("access-request")]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawBody body, CancellationToken cancellationToken)
    {
        try
        {
            await _access.WithdrawAsync(id, body.Token ?? "", cancellationToken);
            return Ok(new { message = "Verzoek ingetrokken." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("inbox")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.Admin}")]
    public async Task<IActionResult> Inbox(CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var items = await _access.ListInboxAsync(
            accessible ?? [],
            _companyAuth.IsAdmin(User),
            cancellationToken);
        return Ok(items.Select(i => new
        {
            i.RequestId,
            i.TargetCompanyId,
            i.TargetCompanyName,
            i.RequesterName,
            i.RequesterFunction,
            i.RequesterEmail,
            requestedRole = i.RequestedRole.ToString(),
            i.RequestedCompanyIds,
            i.Message,
            status = i.Status.ToString(),
            i.CreatedAtUtc,
            i.AgeWorkingDays
        }));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.BranchManager},{JobsyRoles.Admin}")]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] DecideBody? body,
        CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        UserRole? granted = null;
        if (!string.IsNullOrWhiteSpace(body?.GrantedRole)
            && Enum.TryParse<UserRole>(body.GrantedRole, ignoreCase: true, out var parsed))
        {
            granted = parsed;
        }

        try
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            var result = await _access.ApproveAsync(
                id,
                actor.Id,
                actor.Role,
                accessible,
                _companyAuth.IsAdmin(User),
                granted,
                body?.GrantedCompanyIds,
                cancellationToken);
            return Ok(new
            {
                result.RequestId,
                status = result.Status.ToString(),
                result.Message,
                result.CreatedUserId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Conflict bij goedkeuren — probeer opnieuw." });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.BranchManager},{JobsyRoles.Admin}")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] DecideBody? body,
        CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        try
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            var result = await _access.RejectAsync(
                id,
                actor.Id,
                accessible,
                _companyAuth.IsAdmin(User),
                body?.Reason,
                cancellationToken);
            return Ok(new
            {
                result.RequestId,
                status = result.Status.ToString(),
                result.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("admin/escalated")]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<IActionResult> AdminEscalated(CancellationToken cancellationToken)
    {
        var items = await _access.ListEscalatedForAdminAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("admin/ownership-transfers")]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<IActionResult> AdminOwnershipTransfers(CancellationToken cancellationToken)
    {
        var items = await _access.ListOwnershipTransfersForAdminAsync(cancellationToken);
        return Ok(items);
    }

    public sealed record OwnershipLetterBody(string Code);

    [HttpPost("admin/ownership-transfers/{id:guid}/confirm-letter")]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<IActionResult> AdminConfirmOwnershipLetter(
        Guid id,
        [FromBody] OwnershipLetterBody body,
        CancellationToken cancellationToken)
    {
        try
        {
            await _registration.ConfirmOwnershipTransferLetterAsync(id, body.Code ?? "", cancellationToken);
            return Ok(new { message = "Briefcode bevestigd." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Anonymous letter confirm for the ownership-transfer requester (signed by knowing the code from the letter).</summary>
    [HttpPost("ownership-transfers/{id:guid}/confirm-letter")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> ConfirmOwnershipLetter(
        Guid id,
        [FromBody] OwnershipLetterBody body,
        CancellationToken cancellationToken)
    {
        try
        {
            await _registration.ConfirmOwnershipTransferLetterAsync(id, body.Code ?? "", cancellationToken);
            return Ok(new { message = "Briefcode bevestigd. Lobsy-support beoordeelt de overdracht." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
