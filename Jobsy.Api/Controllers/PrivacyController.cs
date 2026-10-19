using Jobsy.Api.Admin;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/privacy")]
[Authorize]
public sealed class PrivacyController : ControllerBase
{
    private readonly IPrivacyDataService _privacy;
    private readonly IPlatformFeatureService _features;
    private readonly ISupportAccessService _supportAccess;
    private readonly IUserLookupService _users;
    private readonly ICandidateProfileResetService _profileReset;
    private readonly ILogger<PrivacyController> _logger;

    public PrivacyController(
        IPrivacyDataService privacy,
        IPlatformFeatureService features,
        ISupportAccessService supportAccess,
        IUserLookupService users,
        ICandidateProfileResetService profileReset,
        ILogger<PrivacyController> logger)
    {
        _privacy = privacy;
        _features = features;
        _supportAccess = supportAccess;
        _users = users;
        _profileReset = profileReset;
        _logger = logger;
    }

    /// <summary>AVG Art. 15 / 20 — export personal data as JSON. Own account only.</summary>
    [HttpGet("export")]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _privacy.ExportAsync(User, cancellationToken);
            return Ok(payload);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Optional (admin setting, default off): UTC dates when support temporarily accessed this subject's data.
    /// </summary>
    [HttpGet("support-access-notes")]
    public async Task<ActionResult<IReadOnlyList<DateTime>>> GetSupportAccessNotes(
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        if (!features.SupportAccessNotifySubject)
        {
            return Ok(Array.Empty<DateTime>());
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dates = await _supportAccess.ListAccessDatesForSubjectAsync(user.Id, 10, cancellationToken);
        return Ok(dates);
    }

    /// <summary>Fixed unsubscribe / account-deletion reason options.</summary>
    [HttpGet("unsubscribe-reasons")]
    public ActionResult<IEnumerable<UnsubscribeReasonOptionDto>> GetUnsubscribeReasons()
    {
        var items = AccountUnsubscribeReasons.All.Select(r =>
            new UnsubscribeReasonOptionDto(
                r.Code,
                r.Label,
                AccountUnsubscribeReasons.RequiresOtherText(r.Code)));
        return Ok(items);
    }

    /// <summary>Account deletion step 1 — store reason and e-mail verification code.</summary>
    [HttpPost("request-unsubscribe")]
    [AdminAuditExempt("OTP request; no admin write")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RequestUnsubscribe(
        [FromBody] RequestUnsubscribeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _privacy.RequestUnsubscribeAsync(
                User,
                request.ReasonCode,
                request.ReasonOther,
                cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Account deletion step 2 — verify code, block account and clean data.</summary>
    [HttpPost("confirm-unsubscribe")]
    [AdminAuditExempt("Self unsubscribe confirm; deletion covered by delete-account")]
    [EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> ConfirmUnsubscribe(
        [FromBody] ConfirmUnsubscribeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _privacy.ConfirmUnsubscribeAsync(User, request.VerificationCode, cancellationToken);
            return Ok(new { message = "Account is afgemeld en gegevens zijn opgeschoond." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return PrivacyFailure(ex, "confirm-unsubscribe");
        }
    }

    /// <summary>
    /// Candidate-only: wipe profile data and restart onboarding; login identity is kept.
    /// </summary>
    [HttpPost("reset-candidate-profile")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    [AdminAudit(AdminAuditKeys.PrivacyCandidateProfileReset, TargetType = AdminAuditKeys.TargetTypes.Account)]
    [EnableRateLimiting("otp-verify")]
    public async Task<ActionResult<ResetCandidateProfileResponse>> ResetCandidateProfile(
        [FromBody] ResetCandidateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var language = Request.Headers["X-Jobsy-Language"].FirstOrDefault();
        try
        {
            var me = await _users.FindByPrincipalAsync(User, cancellationToken);
            var auditContext = HttpContext.RequestServices.GetRequiredService<IAdminAuditContext>();
            auditContext.ForceWrite = true;
            if (me is not null)
            {
                auditContext.TargetId = me.Id.ToString("D");
                auditContext.TargetLabel = $"Kandidaat #{me.Id.ToString("N")[..8]}";
            }

            var result = await _profileReset.ResetAsync(
                User,
                request.ConfirmPhrase,
                language,
                cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return PrivacyFailure(ex, "reset-candidate-profile");
        }
    }

    /// <summary>
    /// AVG Art. 17 — anonymize account data. Requires a prior <c>request-unsubscribe</c>
    /// and the e-mail verification code (same as <c>confirm-unsubscribe</c>).
    /// </summary>
    [HttpPost("delete-account")]
    [AdminAudit(AdminAuditKeys.PrivacyAccountDeleted, TargetType = AdminAuditKeys.TargetTypes.Account)]
    [EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> DeleteAccount(
        [FromBody] ConfirmUnsubscribeRequest? request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.VerificationCode))
        {
            return BadRequest(new
            {
                message = "Bevestig met de e-mailverificatiecode. Vraag eerst een code aan via request-unsubscribe."
            });
        }

        try
        {
            var me = await _users.FindByPrincipalAsync(User, cancellationToken);
            var auditContext = HttpContext.RequestServices.GetRequiredService<IAdminAuditContext>();
            auditContext.ForceWrite = true;
            if (me is not null)
            {
                var shortId = me.Id.ToString("N")[..8];
                auditContext.TargetId = me.Id.ToString("D");
                auditContext.TargetLabel = me.Role == Core.Enums.UserRole.Candidate
                    ? $"Kandidaat #{shortId}"
                    : $"Account #{shortId}";
            }

            await _privacy.ConfirmUnsubscribeAsync(User, request.VerificationCode, cancellationToken);
            return Ok(new { message = "Accountgegevens zijn geanonimiseerd." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return PrivacyFailure(ex, "delete-account");
        }
    }

    private ObjectResult PrivacyFailure(Exception ex, string operation)
    {
        var supportCode = PrivacyOperationErrors.CreateSupportCode();
        _logger.LogError(
            ex,
            "Privacy operation {Operation} failed for authenticated user ({SupportCode}, trace {TraceId})",
            operation,
            supportCode,
            HttpContext.TraceIdentifier);
        return StatusCode(
            StatusCodes.Status500InternalServerError,
            new
            {
                message = PrivacyOperationErrors.GenericMessageNl,
                supportCode,
                referenceCode = supportCode
            });
    }
}
