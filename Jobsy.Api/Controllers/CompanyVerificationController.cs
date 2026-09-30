using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/company-verification")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class CompanyVerificationController : ControllerBase
{
    private readonly ICompanyVerificationFlowService _flow;
    private readonly IUserLookupService _users;

    public CompanyVerificationController(
        ICompanyVerificationFlowService flow,
        IUserLookupService users)
    {
        _flow = flow;
        _users = users;
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var options = await _flow.GetOptionsAsync(user.Id, cancellationToken);
        return Ok(options);
    }

    [HttpPost("email/start")]
    [EnableRateLimiting("verify-start")]
    public async Task<IActionResult> StartEmail(
        [FromBody] EmailStartRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _flow.StartEmailAsync(user.Id, request.Email ?? "", cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { error = result.ErrorCode, message = result.Message });
        }

        return Ok(new
        {
            expiresAtUtc = result.ExpiresAtUtc,
            maskedEmail = result.MaskedEmail
        });
    }

    [HttpPost("email/confirm")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] CodeConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _flow.ConfirmEmailAsync(user.Id, request.Code ?? "", cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { error = result.ErrorCode, message = result.Message });
        }

        return Ok(new { status = result.Status?.ToString() });
    }

    [HttpPost("letter")]
    [EnableRateLimiting("verify-start")]
    public async Task<IActionResult> RequestLetter(
        [FromBody] LetterRequestBody? body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _flow.RequestLetterAsync(user.Id, body?.Resend == true, cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { error = result.ErrorCode, message = result.Message });
        }

        return Ok(new
        {
            letterId = result.LetterId,
            addressMasked = result.AddressMasked,
            expiresAtUtc = result.ExpiresAtUtc,
            sentAtUtc = result.SentAtUtc,
            resendAvailableAtUtc = result.ResendAvailableAtUtc,
            resendsRemaining = result.ResendsRemaining
        });
    }

    [HttpPost("letter/confirm")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> ConfirmLetter(
        [FromBody] CodeConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _flow.ConfirmLetterAsync(user.Id, request.Code ?? "", cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { error = result.ErrorCode, message = result.Message });
        }

        return Ok(new { status = result.Status?.ToString() });
    }

    [HttpPost("manual")]
    [EnableRateLimiting("verify-start")]
    public async Task<IActionResult> RequestManual(
        [FromBody] ManualRequestBody request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _flow.RequestManualAsync(
            user.Id,
            request.Reason ?? "",
            request.Message,
            request.AttachmentIds,
            cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { error = result.ErrorCode, message = result.Message });
        }

        return Ok(new { message = "We reageren binnen 2 werkdagen." });
    }

    public sealed record EmailStartRequest(string? Email);
    public sealed record CodeConfirmRequest(string? Code);
    public sealed record LetterRequestBody(bool? Resend);
    public sealed record ManualRequestBody(string? Reason, string? Message, List<string>? AttachmentIds);
}

[ApiController]
[Route("api/admin/company-verification")]
[Authorize(Roles = JobsyRoles.Admin)]
public sealed class AdminCompanyVerificationController : ControllerBase
{
    private readonly ICompanyVerificationAdminService _admin;
    private readonly ICompanyVerificationFlowService _flow;
    private readonly IUserLookupService _users;

    public AdminCompanyVerificationController(
        ICompanyVerificationAdminService admin,
        ICompanyVerificationFlowService flow,
        IUserLookupService users)
    {
        _admin = admin;
        _flow = flow;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? tab, CancellationToken cancellationToken)
    {
        var items = await _admin.ListQueueAsync(tab, cancellationToken);
        return Ok(items);
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
        => Ok(new { count = await _admin.CountOpenAsync(cancellationToken) });

    [HttpPost("{companyId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid companyId,
        [FromBody] AdminNoteBody? body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await _admin.ApproveAsync(companyId, user.Id, body?.Note, cancellationToken);
        return Ok(new { ok = true });
    }

    [HttpPost("{companyId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid companyId,
        [FromBody] AdminRejectBody body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(body.Reason))
        {
            return BadRequest(new { error = "reason_required", message = "Reden is verplicht." });
        }

        await _admin.RejectAsync(companyId, user.Id, body.Reason, cancellationToken);
        return Ok(new { ok = true });
    }

    [HttpPost("{companyId:guid}/letter")]
    public async Task<IActionResult> SendLetter(Guid companyId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _flow.AdminSendLetterAsync(companyId, user.Id, cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { error = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    [HttpGet("letters/{letterId:guid}/stub-pdf")]
    public async Task<IActionResult> StubPdf(Guid letterId, CancellationToken cancellationToken)
    {
        var pdf = await _admin.GetStubLetterPdfAsync(letterId, cancellationToken);
        if (pdf is null)
        {
            return NotFound();
        }

        return File(pdf, "application/pdf", "testbrief.pdf");
    }

    public sealed record AdminNoteBody(string? Note);
    public sealed record AdminRejectBody(string? Reason);
}
