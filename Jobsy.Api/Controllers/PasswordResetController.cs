using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Jobsy.Core.Email.Model;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/auth/password-reset")]
public sealed class PasswordResetController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IOneTimeLinkService _links;
    private readonly IDeviceSessionService _deviceSessions;
    private readonly IMfaTrustedDeviceService _trustedDevices;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly PasswordResetRequestLimiter _limiter;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(
        JobsyDbContext db,
        IOneTimeLinkService links,
        IDeviceSessionService deviceSessions,
        IMfaTrustedDeviceService trustedDevices,
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        PasswordResetRequestLimiter limiter,
        ILogger<PasswordResetController> logger)
    {
        _db = db;
        _links = links;
        _deviceSessions = deviceSessions;
        _trustedDevices = trustedDevices;
        _mailer = mailer;
        _features = features;
        _limiter = limiter;
        _logger = logger;
    }

    public sealed record PasswordResetRequestBody(string? Email, string? Culture);
    public sealed record PasswordResetCompleteBody(string? Token, string? Password);

    [AllowAnonymous]
    [HttpPost("request")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RequestReset(
        [FromBody] PasswordResetRequestBody request,
        CancellationToken cancellationToken)
    {
        // Always 202 — never reveal whether the account exists.
        var normalized = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var key = _limiter.KeyForEmail(string.IsNullOrWhiteSpace(normalized) ? "_" : normalized);
        var now = DateTime.UtcNow;
        var allowed = _limiter.TryAllow(key, now);

        // Constant-ish work: always touch hasher + DB path shape.
        _ = JobsyPasswordHasher.Hash("timing-dummy-password-reset!!");

        User? user = null;
        if (allowed
            && normalized.Contains('@', StringComparison.Ordinal)
            && normalized.Length is >= 3 and <= 254)
        {
            user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);
        }

        Guid? logUserId = user?.Id;
        var logLabel = user is null ? "unknown" : user.Id.ToString("N");

        if (allowed && user is { IsActive: true })
        {
            var hasLocal = await _db.LocalAuthCredentials.AsNoTracking()
                .AnyAsync(c => c.UserId == user.Id, cancellationToken);
            var externalProviders = await _db.UserExternalLogins.AsNoTracking()
                .Where(l => l.UserId == user.Id)
                .Select(l => l.Provider)
                .ToListAsync(cancellationToken);

            var features = await _features.GetAsync(cancellationToken);
            var culture = EmailCulture.ForLanguage(request.Culture);

            if (hasLocal || externalProviders.Count == 0)
            {
                var created = await _links.CreateAsync(
                    OneTimeLinkPurpose.PasswordReset,
                    user.Id,
                    companyId: null,
                    user.Email,
                    OneTimeLinkRules.PasswordResetLifetime,
                    cancellationToken: cancellationToken);
                var mail = TransactionalEmails.PasswordReset(
                    features.PublicWebBaseUrl,
                    created.Token,
                    culture);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _mailer.SendAsync(mail, user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Password reset mail failed for user {UserId}", user.Id);
                    }
                });
            }
            else
            {
                var provider = externalProviders
                    .Select(p => p?.Trim().ToLowerInvariant())
                    .FirstOrDefault(p => p is "microsoft" or "entra" or "google")
                    ?? externalProviders[0];
                var label = provider is "google" ? "Google" : "Microsoft";
                var mail = TransactionalEmails.PasswordResetExternalOnly(
                    features.PublicWebBaseUrl,
                    label,
                    culture);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _mailer.SendAsync(mail, user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Password reset external-only mail failed for user {UserId}", user.Id);
                    }
                });
            }
        }

        _logger.LogInformation("auth.password_reset.requested user={User}", logLabel);
        _ = logUserId;
        return Accepted();
    }

    [AllowAnonymous]
    [HttpGet("peek")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<object>> Peek(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        var peek = await _links.PeekAsync(OneTimeLinkPurpose.PasswordReset, token ?? "", cancellationToken);
        if (!peek.Valid)
        {
            return Ok(new { valid = false });
        }

        return Ok(new
        {
            valid = true,
            maskedEmail = peek.MaskedEmail,
            expiresAtUtc = peek.ExpiresAtUtc
        });
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Complete(
        [FromBody] PasswordResetCompleteBody request,
        CancellationToken cancellationToken)
    {
        try
        {
            RegistrationPasswordRules.Validate(request.Password, required: true);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var link = await _links.ConsumeAsync(
            OneTimeLinkPurpose.PasswordReset,
            request.Token ?? "",
            cancellationToken);
        if (link is null || link.UserId is not Guid userId)
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        var email = string.IsNullOrWhiteSpace(link.Email)
            ? user.Email.Trim().ToLowerInvariant()
            : link.Email;
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        var hash = JobsyPasswordHasher.Hash(request.Password!);

        if (credential is null)
        {
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = hash,
                FailedLoginCount = 0,
                LockoutUntil = null,
                LockoutCount = 0
            });
        }
        else
        {
            credential.Email = email;
            credential.PasswordHash = hash;
            credential.FailedLoginCount = 0;
            credential.LockoutUntil = null;
            credential.LockoutCount = 0;
        }

        user.SessionVersion++;
        user.MfaFailedCount = 0;
        user.MfaLockoutUntilUtc = null;

        await _db.SaveChangesAsync(cancellationToken);

        await _deviceSessions.RevokeAllAsync(
            user.Id,
            "password-reset",
            bumpSessionVersion: false,
            cancellationToken);
        await _trustedDevices.RevokeAllForUserAsync(user.Id, cancellationToken);

        try
        {
            var features = await _features.GetAsync(cancellationToken);
            var mail = TransactionalEmails.PasswordChanged(features.PublicWebBaseUrl, DateTime.UtcNow);
            await _mailer.SendAsync(mail, user.Email, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PasswordChanged mail failed for user {UserId}", user.Id);
        }

        _logger.LogInformation("auth.password_reset.completed userId={UserId}", user.Id);
        return Ok(new { ok = true });
    }
}
