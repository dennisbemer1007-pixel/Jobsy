using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/auth/mfa")]
public sealed class MfaController : ControllerBase
{
    public const int MaxUserFailuresBeforeLockout = 10;
    public static readonly TimeSpan MfaLockoutDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MfaLockoutMailCooldown = TimeSpan.FromHours(24);

    private readonly JobsyDbContext _db;
    private readonly ISecretProtector _secrets;
    private readonly IDeviceSessionService _deviceSessions;
    private readonly IMfaTrustedDeviceService _trustedDevices;
    private readonly IConfiguration _configuration;
    private readonly MfaChallengeService _challenges;
    private readonly ITotpVerifier _totp;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<MfaController> _logger;

    public MfaController(
        JobsyDbContext db,
        ISecretProtector secrets,
        IDeviceSessionService deviceSessions,
        IMfaTrustedDeviceService trustedDevices,
        IConfiguration configuration,
        MfaChallengeService challenges,
        ITotpVerifier totp,
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        ILogger<MfaController> logger)
    {
        _db = db;
        _secrets = secrets;
        _deviceSessions = deviceSessions;
        _trustedDevices = trustedDevices;
        _configuration = configuration;
        _challenges = challenges;
        _totp = totp;
        _mailer = mailer;
        _features = features;
        _logger = logger;
    }

    [HttpPost("state")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MfaStateResponse>> State(
        [FromBody] MfaStateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_challenges.TryGet(request.ChallengeToken, out var challenge))
        {
            return Unauthorized(new { code = "challenge_expired" });
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == challenge.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { code = "challenge_expired" });
        }

        if (user.MfaLockoutUntilUtc is DateTime until && until > DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "mfa_locked", retryAtUtc = until });
        }

        return Ok(new MfaStateResponse(
            user.AuthenticatorEnabled,
            user.Email,
            EmailMask.Mask(user.Email)));
    }

    [HttpPost("enroll")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MfaEnrollmentResponse>> Enroll(
        [FromBody] MfaEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!_challenges.TryGet(request.ChallengeToken, out var challenge))
        {
            return Unauthorized(new { code = "challenge_expired" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == challenge.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { code = "challenge_expired" });
        }

        if (user.MfaLockoutUntilUtc is DateTime until && until > DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "mfa_locked", retryAtUtc = until });
        }

        if (user.AuthenticatorEnabled)
        {
            return Conflict(new { enrolled = true, message = "Authenticator is al ingesteld." });
        }

        var secret = _secrets.Unprotect(user.AuthenticatorSecret);
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = TotpAuthenticator.GenerateSecret();
            user.AuthenticatorSecret = _secrets.Protect(secret);
            user.AuthenticatorEnabled = false;
            user.RecoveryCodesHash = null;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var provisioningUri = TotpAuthenticator.BuildProvisioningUri(user.Email, secret);
        return Ok(new MfaEnrollmentResponse(
            secret,
            provisioningUri,
            TotpQrCode.ToSvgDataUri(provisioningUri)));
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LocalLoginResponse>> Verify(
        [FromBody] MfaVerifyRequest request,
        CancellationToken cancellationToken)
    {
        if (!_challenges.TryGet(request.ChallengeToken, out var challenge))
        {
            return Unauthorized(new { code = "challenge_expired" });
        }

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Id == challenge.UserId && u.IsActive, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { code = "challenge_expired" });
        }

        var now = DateTime.UtcNow;
        if (user.MfaLockoutUntilUtc is DateTime lockedUntil && lockedUntil > now)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "mfa_locked",
                retryAtUtc = lockedUntil
            });
        }

        var secret = _secrets.Unprotect(user.AuthenticatorSecret);
        string[] recoveryCodes = [];
        var usedRecovery = false;
        int? recoveryCodesLeft = null;
        var method = (request.Method ?? string.Empty).Trim().ToLowerInvariant();
        var preferRecovery = method == "recovery"
            || (string.IsNullOrWhiteSpace(method) && !string.IsNullOrWhiteSpace(request.RecoveryCode));

        if (!user.AuthenticatorEnabled)
        {
            var enrollResult = await _totp.VerifyAsync(
                user, secret, StripSpaces(request.Code), now, cancellationToken);
            if (!enrollResult.Ok)
            {
                return await FailCodeAsync(user, request.ChallengeToken, cancellationToken);
            }

            recoveryCodes = MfaRecoveryCodes.Generate();
            user.RecoveryCodesHash = JsonSerializer.Serialize(recoveryCodes.Select(MfaRecoveryCodes.Hash));
            user.AuthenticatorEnabled = true;
            user.AuthenticatorEnrolledAtUtc = now;
        }
        else if (preferRecovery)
        {
            usedRecovery = TryUseRecoveryCode(user, request.RecoveryCode);
            if (!usedRecovery)
            {
                return await FailCodeAsync(user, request.ChallengeToken, cancellationToken);
            }

            var hashes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodesHash ?? "[]") ?? [];
            recoveryCodesLeft = hashes.Count;
            await SendRecoveryCodeUsedMailAsync(user, recoveryCodesLeft.Value, cancellationToken);
        }
        else
        {
            var totpResult = await _totp.VerifyAsync(
                user, secret, StripSpaces(request.Code), now, cancellationToken);
            if (!totpResult.Ok)
            {
                return await FailCodeAsync(user, request.ChallengeToken, cancellationToken);
            }
        }

        user.MfaFailedCount = 0;
        user.MfaLockoutUntilUtc = null;

        string? trustRaw = null;
        // Trust only after successful TOTP/recovery on an already-enrolled account (not first enroll).
        if (request.TrustDevice && recoveryCodes.Length == 0)
        {
            var created = await _trustedDevices.CreateAsync(
                user.Id,
                Request.Headers.UserAgent.ToString(),
                user.SessionVersion,
                cancellationToken);
            trustRaw = created.RawToken;
        }

        var sessionToken = CreateLocalSessionToken(user.Email, user.Id);
        Guid? deviceSessionId = null;
        string? deviceRefresh = null;
        DateTime? deviceExpires = null;
        if (challenge.RememberDevice)
        {
            var device = await _deviceSessions.CreateAsync(
                user.Id,
                Request.Headers.UserAgent.ToString(),
                mfaVerified: true,
                cancellationToken: cancellationToken);
            deviceSessionId = device.DeviceSessionId;
            deviceRefresh = device.RefreshToken;
            deviceExpires = device.ExpiresAtUtc;
        }

        var showHowTo = user.Role == UserRole.Candidate && user.LastLoginAtUtc is null;
        user.LastLoginAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        _challenges.Consume(request.ChallengeToken);

        var companyIds = user.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
        if (user.CompanyId is Guid primary && !companyIds.Contains(primary))
        {
            companyIds.Insert(0, primary);
        }

        var hasApps = await _db.Applications.AsNoTracking()
            .AnyAsync(a => a.CandidateUserId == user.Id, cancellationToken);
        var hasSalesReferral = user.CompanyId is Guid companyId
            && await _db.Companies.AsNoTracking()
                .AnyAsync(c => c.Id == companyId && c.ReferredBySalesManagerUserId != null, cancellationToken);

        if (usedRecovery)
        {
            _logger.LogInformation("mfa.recovery.used userId={UserId}", user.Id);
        }

        var grouped = recoveryCodes.Length == 0
            ? recoveryCodes
            : recoveryCodes.Select(MfaRecoveryCodes.FormatGrouped).ToArray();

        return Ok(new LocalLoginResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            companyIds,
            showHowTo,
            hasApps,
            hasSalesReferral,
            sessionToken,
            user.SessionVersion,
            deviceSessionId,
            deviceRefresh,
            deviceExpires,
            user.Id,
            MfaVerified: true,
            RecoveryCodes: grouped,
            SchoolId: user.SchoolId,
            RecoveryCodesLeft: recoveryCodesLeft,
            UsedRecoveryCode: usedRecovery,
            MfaTrustToken: trustRaw,
            AuthMethod: "password+mfa"));
    }

    [HttpPost("recovery-codes/regenerate")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<object>> RegenerateRecoveryCodes(
        [FromBody] MfaRegenerateRecoveryCodesRequest request,
        CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null || !user.AuthenticatorEnabled)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        if (user.MfaLockoutUntilUtc is DateTime lockedUntil && lockedUntil > now)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "mfa_locked",
                retryAtUtc = lockedUntil
            });
        }

        var secret = _secrets.Unprotect(user.AuthenticatorSecret);
        var totpResult = await _totp.VerifyAsync(user, secret, StripSpaces(request.Code), now, cancellationToken);
        if (!totpResult.Ok)
        {
            return await FailCodeAsync(user, challengeToken: null, cancellationToken);
        }

        var codes = MfaRecoveryCodes.Generate();
        user.RecoveryCodesHash = JsonSerializer.Serialize(codes.Select(MfaRecoveryCodes.Hash));
        user.MfaFailedCount = 0;
        user.MfaLockoutUntilUtc = null;
        await _trustedDevices.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var features = await _features.GetAsync(cancellationToken);
            var mail = TransactionalEmails.RecoveryCodesRegenerated(features.PublicWebBaseUrl);
            await _mailer.SendAsync(mail, user.Email, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recovery regenerate mail failed for user {UserId}", user.Id);
        }

        _logger.LogInformation("mfa.recovery.regenerated userId={UserId}", user.Id);
        return Ok(new
        {
            recoveryCodes = codes.Select(MfaRecoveryCodes.FormatGrouped).ToArray()
        });
    }

    private async Task<ActionResult> FailCodeAsync(
        User user,
        string? challengeToken,
        CancellationToken cancellationToken)
    {
        user.MfaFailedCount++;
        var now = DateTime.UtcNow;
        if (user.MfaFailedCount >= MaxUserFailuresBeforeLockout)
        {
            user.MfaLockoutUntilUtc = now.Add(MfaLockoutDuration);
            user.MfaFailedCount = 0;
            await _db.SaveChangesAsync(cancellationToken);
            await SendMfaLockoutMailAsync(user, cancellationToken);
            if (!string.IsNullOrWhiteSpace(challengeToken))
            {
                _challenges.Consume(challengeToken);
            }

            _logger.LogInformation("mfa.locked userId={UserId}", user.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "mfa_locked",
                retryAtUtc = user.MfaLockoutUntilUtc
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("mfa.verify.failed userId={UserId}", user.Id);

        if (!string.IsNullOrWhiteSpace(challengeToken) && _challenges.RegisterFailure(challengeToken))
        {
            return Unauthorized(new { code = "challenge_expired" });
        }

        return Unauthorized(new { code = "invalid_code" });
    }

    private async Task SendMfaLockoutMailAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (user.LastMfaLockoutMailAtUtc is DateTime last
            && last > now - MfaLockoutMailCooldown)
        {
            return;
        }

        try
        {
            var features = await _features.GetAsync(cancellationToken);
            var mail = TransactionalEmails.MfaLockout(features.PublicWebBaseUrl);
            await _mailer.SendAsync(mail, user.Email, cancellationToken: cancellationToken);
            user.LastMfaLockoutMailAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MFA lockout mail failed for user {UserId}", user.Id);
        }
    }

    private async Task SendRecoveryCodeUsedMailAsync(User user, int left, CancellationToken cancellationToken)
    {
        try
        {
            var features = await _features.GetAsync(cancellationToken);
            var mail = TransactionalEmails.RecoveryCodeUsed(features.PublicWebBaseUrl, left);
            await _mailer.SendAsync(mail, user.Email, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recovery-code-used mail failed for user {UserId}", user.Id);
        }
    }

    private string? CreateLocalSessionToken(string email, Guid userId)
    {
        var secret = JobsyLocalSessionToken.ResolveSigningKey(
            _configuration["JobsyAuth:LocalSessionSigningKey"],
            _configuration["JobsyAuth:DevelopmentAuthSecret"]);
        return string.IsNullOrWhiteSpace(secret) ? null : JobsyLocalSessionToken.Create(email, userId, secret);
    }

    private static string? StripSpaces(string? code)
        => string.IsNullOrWhiteSpace(code) ? code : code.Replace(" ", string.Empty, StringComparison.Ordinal);

    private static bool TryUseRecoveryCode(User user, string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode) || string.IsNullOrWhiteSpace(user.RecoveryCodesHash))
        {
            return false;
        }

        var hashes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodesHash) ?? [];
        var candidate = MfaRecoveryCodes.Hash(rawCode);
        var index = hashes.FindIndex(hash =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(hash),
                Encoding.ASCII.GetBytes(candidate)));
        if (index < 0)
        {
            return false;
        }

        hashes.RemoveAt(index);
        user.RecoveryCodesHash = JsonSerializer.Serialize(hashes);
        return true;
    }
}
