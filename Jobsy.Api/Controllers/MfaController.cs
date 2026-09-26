using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
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
    private readonly JobsyDbContext _db;
    private readonly ISecretProtector _secrets;
    private readonly IDeviceSessionService _deviceSessions;
    private readonly IPlatformFeatureService _features;
    private readonly IConfiguration _configuration;
    private readonly MfaChallengeService _challenges;

    public MfaController(
        JobsyDbContext db,
        ISecretProtector secrets,
        IDeviceSessionService deviceSessions,
        IPlatformFeatureService features,
        IConfiguration configuration,
        MfaChallengeService challenges)
    {
        _db = db;
        _secrets = secrets;
        _deviceSessions = deviceSessions;
        _features = features;
        _configuration = configuration;
        _challenges = challenges;
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
            return Unauthorized(new { message = "De inlogcontrole is verlopen. Log opnieuw in." });
        }

        var features = await _features.GetAsync(cancellationToken);
        if (!features.AuthenticatorEnabled)
        {
            return BadRequest(new { message = "Authenticator is tijdelijk niet beschikbaar." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == challenge.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "De inlogcontrole is verlopen. Log opnieuw in." });
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

        return Ok(new MfaEnrollmentResponse(secret, TotpAuthenticator.BuildProvisioningUri(user.Email, secret)));
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
            return Unauthorized(new { message = "De inlogcontrole is verlopen. Log opnieuw in." });
        }

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Id == challenge.UserId && u.IsActive, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "De inlogcontrole is verlopen. Log opnieuw in." });
        }

        var secret = _secrets.Unprotect(user.AuthenticatorSecret);
        var usedRecovery = TryUseRecoveryCode(user, request.RecoveryCode);
        if (!usedRecovery && !TotpAuthenticator.VerifyCode(secret, request.Code?.Trim(), DateTime.UtcNow))
        {
            return Unauthorized(new { message = "De authenticatorcode is onjuist." });
        }

        string[] recoveryCodes = [];
        if (!user.AuthenticatorEnabled)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                return BadRequest(new { message = "Start eerst met het instellen van je authenticator." });
            }

            recoveryCodes = GenerateRecoveryCodes();
            user.RecoveryCodesHash = JsonSerializer.Serialize(recoveryCodes.Select(HashRecoveryCode));
            user.AuthenticatorEnabled = true;
            user.AuthenticatorEnrolledAtUtc = DateTime.UtcNow;
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
                cancellationToken,
                mfaVerified: true);
            deviceSessionId = device.DeviceSessionId;
            deviceRefresh = device.RefreshToken;
            deviceExpires = device.ExpiresAtUtc;
        }

        var showHowTo = user.Role == UserRole.Candidate && user.LastLoginAtUtc is null;
        user.LastLoginAtUtc = DateTime.UtcNow;
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
            RecoveryCodes: recoveryCodes));
    }

    private string? CreateLocalSessionToken(string email, Guid userId)
    {
        var secret = JobsyLocalSessionToken.ResolveSigningKey(
            _configuration["JobsyAuth:LocalSessionSigningKey"],
            _configuration["JobsyAuth:DevelopmentAuthSecret"]);
        return string.IsNullOrWhiteSpace(secret) ? null : JobsyLocalSessionToken.Create(email, userId, secret);
    }

    private static string[] GenerateRecoveryCodes()
        => Enumerable.Range(0, 10)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)))
            .ToArray();

    private static string HashRecoveryCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant())));

    private static bool TryUseRecoveryCode(User user, string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode) || string.IsNullOrWhiteSpace(user.RecoveryCodesHash))
        {
            return false;
        }

        var hashes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodesHash) ?? [];
        var candidate = HashRecoveryCode(rawCode);
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
