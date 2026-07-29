using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/privacy")]
[Authorize]
public sealed class PrivacyController : ControllerBase
{
    private readonly IPrivacyDataService _privacy;

    public PrivacyController(IPrivacyDataService privacy)
    {
        _privacy = privacy;
    }

    /// <summary>AVG Art. 15 / 20 — export personal data as JSON.</summary>
    [HttpGet("export")]
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

    /// <summary>Fixed unsubscribe reason options for candidates.</summary>
    [HttpGet("unsubscribe-reasons")]
    [Authorize(Roles = "Candidate")]
    public ActionResult<IEnumerable<UnsubscribeReasonOptionDto>> GetUnsubscribeReasons()
    {
        var items = AccountUnsubscribeReasons.All.Select(r =>
            new UnsubscribeReasonOptionDto(
                r.Code,
                r.Label,
                AccountUnsubscribeReasons.RequiresOtherText(r.Code)));
        return Ok(items);
    }

    /// <summary>Candidate unsubscribe step 1 — store reason and e-mail verification code.</summary>
    [HttpPost("request-unsubscribe")]
    [Authorize(Roles = "Candidate")]
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

    /// <summary>Candidate unsubscribe step 2 — verify code, block account and clean data.</summary>
    [HttpPost("confirm-unsubscribe")]
    [Authorize(Roles = "Candidate")]
    [EnableRateLimiting("auth")]
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
    }

    /// <summary>AVG Art. 17 — anonymize / erase account data.</summary>
    [HttpPost("delete-account")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        try
        {
            await _privacy.DeleteOrAnonymizeAsync(User, cancellationToken);
            return Ok(new { message = "Accountgegevens zijn geanonimiseerd." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
