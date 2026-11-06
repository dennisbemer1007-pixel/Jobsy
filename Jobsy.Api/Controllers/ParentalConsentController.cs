using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/parental-consent")]
public sealed class ParentalConsentController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;

    public ParentalConsentController(JobsyDbContext db, IPlatformFeatureService features)
    {
        _db = db;
        _features = features;
    }

    public sealed record ParentalConsentPreviewResponse(
        bool Valid,
        string? ChildFirstName = null,
        DateTime? ExpiresAtUtc = null);

    public sealed record ParentalConsentConfirmRequest(string? Token);

    public sealed record ParentalConsentConfirmResponse(bool Ok, string? ChildFirstName = null);

    [AllowAnonymous]
    [HttpGet("preview")]
    [EnableRateLimiting("otp-verify")]
    public async Task<ActionResult<ParentalConsentPreviewResponse>> Preview(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        var user = await FindValidUserAsync(token, cancellationToken);
        if (user is null)
        {
            return Ok(new ParentalConsentPreviewResponse(Valid: false));
        }

        return Ok(new ParentalConsentPreviewResponse(
            Valid: true,
            ChildFirstName: FirstNameOrNull(user.FirstName),
            ExpiresAtUtc: user.ParentalConsentTokenExpiresAt));
    }

    [AllowAnonymous]
    [HttpPost("confirm")]
    [EnableRateLimiting("otp-verify")]
    public async Task<ActionResult<ParentalConsentConfirmResponse>> ConfirmPost(
        [FromBody] ParentalConsentConfirmRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        var tokenHash = VerificationCodes.Hash(request.Token.Trim());
        var now = DateTime.UtcNow;
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.ParentalConsentTokenHash == tokenHash
                 && u.ParentalConsentTokenExpiresAt != null
                 && u.ParentalConsentTokenExpiresAt > now,
            cancellationToken);
        if (user is null)
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        if (user.ParentalConsentAt is not null)
        {
            // Idempotent: already confirmed (token kept until expiry for no-op retries).
            return Ok(new ParentalConsentConfirmResponse(Ok: true, ChildFirstName: FirstNameOrNull(user.FirstName)));
        }

        if (!CandidateConsentRules.RequiresParentalConsent(user))
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        user.ParentalConsentAt = now;
        // Keep hash until ExpiresAt so a second POST is a no-op success (09.3).
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ParentalConsentConfirmResponse(Ok: true, ChildFirstName: FirstNameOrNull(user.FirstName)));
    }

    /// <summary>
    /// Legacy GET links redirect to the website page. Never writes to the database.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("confirm")]
    [EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> ConfirmGet(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = (features.PublicWebBaseUrl ?? "https://lobsy.nl").TrimEnd('/');
        var target = string.IsNullOrWhiteSpace(token)
            ? $"{baseUrl}/toestemming"
            : $"{baseUrl}/toestemming?t={Uri.EscapeDataString(token.Trim())}";
        return Redirect(target);
    }

    private async Task<Core.Entities.User?> FindValidUserAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var tokenHash = VerificationCodes.Hash(token.Trim());
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.ParentalConsentTokenHash == tokenHash
                 && u.ParentalConsentTokenExpiresAt != null
                 && u.ParentalConsentTokenExpiresAt > now,
            cancellationToken);
        if (user is null || !CandidateConsentRules.RequiresParentalConsent(user))
        {
            return null;
        }

        return user;
    }

    private static string? FirstNameOrNull(string? firstName)
        => string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();
}
