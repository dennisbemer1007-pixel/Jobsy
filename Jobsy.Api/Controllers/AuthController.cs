using System.Security.Cryptography;
using System.Text;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IIntegrationCredentialService _credentials;
    private readonly IAmbassadeurAttributionService _ambassadeurAttribution;
    private readonly IHostEnvironment _environment;
    private readonly IDeviceSessionService _deviceSessions;
    private readonly IEmailService _email;
    private readonly MfaChallengeService _mfaChallenges;

    public AuthController(
        JobsyDbContext db,
        IConfiguration configuration,
        IIntegrationCredentialService credentials,
        IAmbassadeurAttributionService ambassadeurAttribution,
        IHostEnvironment environment,
        IDeviceSessionService deviceSessions,
        IEmailService email,
        MfaChallengeService mfaChallenges)
    {
        _db = db;
        _configuration = configuration;
        _credentials = credentials;
        _ambassadeurAttribution = ambassadeurAttribution;
        _environment = environment;
        _deviceSessions = deviceSessions;
        _email = email;
        _mfaChallenges = mfaChallenges;
    }

    /// <summary>
    /// Validates a local registration credential (hashed password).
    /// Used by the Web login page when the email is not a seeded DemoUser.
    /// </summary>
    [HttpPost("local-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LocalLoginResponse>> LocalLogin(
        [FromBody] LocalLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "E-mail en wachtwoord zijn verplicht." });
        }

        var email = LoginIdentity.Normalize(request.Email);
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        var genericError = new { message = "Ongeldige e-mail of wachtwoord." };
        var now = DateTime.UtcNow;
        if (credential is null)
        {
            return Unauthorized(genericError);
        }

        if (credential.LockoutUntil is DateTime lockedUntil && lockedUntil > now)
        {
            return Unauthorized(genericError);
        }

        if (!JobsyPasswordHasher.Verify(request.Password, credential.PasswordHash))
        {
            credential.FailedLoginCount++;
            var lockoutStarted = false;
            var duration = LoginLockoutRules.LockoutDuration(credential.FailedLoginCount);
            if (duration > TimeSpan.Zero)
            {
                credential.LockoutUntil = now.Add(duration);
                lockoutStarted = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (lockoutStarted)
            {
                try
                {
                    await _email.SendAsync(new EmailMessage(
                        credential.Email,
                        "Je Lobsy-account is tijdelijk geblokkeerd",
                        "<p>Er zijn meerdere mislukte inlogpogingen gedaan. Je account is tijdelijk geblokkeerd. Was je dit niet zelf? Kies dan na de blokkade een nieuw wachtwoord.</p>",
                        "AccountLockout"),
                        cancellationToken);
                }
                catch
                {
                    // The lockout must never depend on e-mail delivery.
                }
            }

            return Unauthorized(genericError);
        }

        credential.FailedLoginCount = 0;
        credential.LockoutUntil = null;
        if (JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(request.Password);
        }
        await _db.SaveChangesAsync(cancellationToken);

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Id == credential.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(genericError);
        }

        if (user.AuthenticatorEnabled || MfaPolicy.IsRequired(user.Role))
        {
            return Ok(new LocalLoginResponse(
                user.Email,
                user.FullName,
                user.Role.ToString(),
                user.CompanyId,
                [],
                RequiresMfa: true,
                MfaEnrolled: user.AuthenticatorEnabled,
                MfaChallengeToken: _mfaChallenges.Create(user, request.RememberDevice, localPassword: true),
                UserId: user.Id));
        }

        var flags = await BuildFlagsAsync(user, cancellationToken);
        var sessionToken = CreateLocalSessionToken(user.Email, user.Id);
        Guid? deviceSessionId = null;
        string? deviceRefresh = null;
        DateTime? deviceExpires = null;
        if (request.RememberDevice)
        {
            var device = await _deviceSessions.CreateAsync(
                user.Id,
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            deviceSessionId = device.DeviceSessionId;
            deviceRefresh = device.RefreshToken;
            deviceExpires = device.ExpiresAtUtc;
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new LocalLoginResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            flags.CompanyIds,
            flags.ShowCandidateHowTo,
            flags.HasCandidateApplications,
            flags.HasSalesReferral,
            sessionToken,
            user.SessionVersion,
            deviceSessionId,
            deviceRefresh,
            deviceExpires,
            user.Id));
    }

    /// <summary>
    /// Upserts an external (Google/Entra) identity into Jobsy Users.
    /// Prefers Provider+Subject (Entra OID) over e-mail so IdP e-mail drift does not orphan accounts.
    /// New users become Candidate; invited managers keep their DB role.
    /// Requires the shared JobsyAuth development/provision secret (server-to-server).
    /// </summary>
    [HttpPost("ensure-external")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<EnsureExternalUserResponse>> EnsureExternal(
        [FromBody] EnsureExternalUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrustedProvisionCaller())
        {
            return Unauthorized(new { message = "Ongeldige provision-secret." });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "E-mail is verplicht." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var fullName = string.IsNullOrWhiteSpace(request.FullName)
            ? email
            : request.FullName.Trim();
        var provider = NormalizeExternalProvider(request.Provider);
        var subject = string.IsNullOrWhiteSpace(request.ProviderSubject)
            ? null
            : request.ProviderSubject.Trim();

        User? user = null;
        var isNew = false;

        // 1) Stable IdP subject wins (survives e-mail / UPN changes).
        if (provider is not null && subject is not null)
        {
            var link = await _db.UserExternalLogins
                .Include(l => l.User!)
                .ThenInclude(u => u.CompanyMemberships)
                .FirstOrDefaultAsync(
                    l => l.Provider == provider && l.ProviderSubject == subject,
                    cancellationToken);
            if (link?.User is not null)
            {
                user = link.User;
            }
        }

        // 2) First-time bind: match verified e-mail to an existing Lobsy user, then store OID.
        if (user is null)
        {
            user = await _db.Users
                .Include(u => u.CompanyMemberships)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
        }

        if (user is null)
        {
            isNew = true;
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = fullName,
                Role = UserRole.Candidate,
                IsActive = true,
                // Same acceptance stamp as passwordless e-mail sign-up (external IdP consent covers terms).
                TermsAcceptedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.ReferralCode))
            {
                await _ambassadeurAttribution.TryAttributeCandidateAsync(
                    user.Id, request.ReferralCode, cancellationToken);
            }
        }
        else
        {
            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Account is niet actief." });
            }

            if (!string.IsNullOrWhiteSpace(fullName)
                && !string.Equals(user.FullName, fullName, StringComparison.Ordinal)
                && fullName != email)
            {
                user.FullName = fullName;
            }

            // First login after invite may still carry a referral cookie — attribute once if unset.
            if (user.Role == UserRole.Candidate
                && user.ReferredByAmbassadeurUserId is null
                && !string.IsNullOrWhiteSpace(request.ReferralCode))
            {
                await _ambassadeurAttribution.TryAttributeCandidateAsync(
                    user.Id, request.ReferralCode, cancellationToken);
            }
        }

        if (provider is not null && subject is not null)
        {
            // First-time OID bind to an existing privileged account is only allowed when the
            // verified e-mail from the IdP matches the stored Lobsy e-mail exactly (already
            // enforced above via email lookup). Refuse bind when the subject is already linked
            // elsewhere (anti link-stealing). Residual risk requires compromising the
            // ExternalProvisionSecret — treated as full server trust.
            await EnsureExternalLoginBoundAsync(user.Id, provider, subject, email, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var flags = await BuildFlagsAsync(user, cancellationToken);
        // IdP (Entra/Google/…) already enforced MFA — never challenge again in Lobsy.
        if (provider is null && (user.AuthenticatorEnabled || MfaPolicy.IsRequired(user.Role)))
        {
            return Ok(new EnsureExternalUserResponse(
                user.Email,
                user.FullName,
                user.Role.ToString(),
                user.CompanyId,
                flags.CompanyIds,
                isNew,
                flags.ShowCandidateHowTo,
                flags.HasCandidateApplications,
                flags.HasSalesReferral,
                UserId: user.Id,
                RequiresMfa: true,
                MfaEnrolled: user.AuthenticatorEnabled,
                MfaChallengeToken: _mfaChallenges.Create(user, request.RememberDevice)));
        }

        var sessionToken = CreateLocalSessionToken(user.Email, user.Id);
        string? handoffCode = null;
        // Always issue a handoff for external login so iOS standalone can finish in-scope.
        var handoff = await _deviceSessions.CreateHandoffAsync(
            user.Id,
            request.RememberDevice,
            request.ReturnUrl,
            request.UserAgent ?? Request.Headers.UserAgent.ToString(),
            cancellationToken);
        handoffCode = handoff.Code;

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var authMethod = provider is null ? null : $"external:{provider}";
        return Ok(new EnsureExternalUserResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            flags.CompanyIds,
            isNew,
            flags.ShowCandidateHowTo,
            flags.HasCandidateApplications,
            flags.HasSalesReferral,
            sessionToken,
            user.SessionVersion,
            handoffCode,
            user.Id,
            AuthMethod: authMethod));
    }

    /// <summary>
    /// Starts a passwordless e-mail code challenge for candidate sign-up / sign-in.
    /// Response shape is identical for new, candidate, non-candidate and invalid addresses.
    /// </summary>
    [HttpPost("email-code/start")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<EmailCodeStartResponse>> StartEmailCode(
        [FromBody] EmailCodeStartRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrustedProvisionCaller())
        {
            return Unauthorized(new { message = "Ongeldige provision-secret." });
        }

        var email = LoginIdentity.Normalize(request.Email);
        var dummy = new EmailCodeStartResponse(Guid.NewGuid());
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            return Accepted(dummy);
        }

        var now = DateTime.UtcNow;
        var stale = await _db.EmailSignInChallenges
            .Where(c => c.CreatedAtUtc < now.AddHours(-24))
            .ToListAsync(cancellationToken);
        if (stale.Count > 0)
        {
            _db.EmailSignInChallenges.RemoveRange(stale);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var window15 = now.AddMinutes(-15);
        var dayStart = now.AddHours(-24);
        var starts15 = await _db.EmailSignInChallenges.AsNoTracking()
            .CountAsync(c => c.EmailNormalized == email && c.CreatedAtUtc >= window15, cancellationToken);
        var startsDay = await _db.EmailSignInChallenges.AsNoTracking()
            .CountAsync(c => c.EmailNormalized == email && c.CreatedAtUtc >= dayStart, cancellationToken);
        // Count non-candidate reminder attempts via PlatformLogs would be heavy; instead track
        // throttle rows even when we only send a password reminder (dummy challenges without hash).
        if (starts15 >= 3 || startsDay >= 10)
        {
            return Conflict(new { message = "too_many_codes" });
        }

        var culture = JobsyLanguages.Normalize(request.Culture);
        var baseUrl = string.IsNullOrWhiteSpace(_configuration["PublicWebBaseUrl"])
            ? "https://lobsy.nl"
            : _configuration["PublicWebBaseUrl"]!.Trim();
        var firstName = string.IsNullOrWhiteSpace(request.FirstName)
            ? null
            : request.FirstName.Trim();
        if (firstName is { Length: > 60 })
        {
            firstName = firstName[..60];
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (user is not null && user.Role != UserRole.Candidate)
        {
            // Throttle row without a usable code (cannot verify).
            _db.EmailSignInChallenges.Add(new EmailSignInChallenge
            {
                Id = Guid.NewGuid(),
                EmailNormalized = email,
                CodeHash = VerificationCodes.Hash(Guid.NewGuid().ToString("N")[..6]),
                Purpose = EmailSignInPurpose.SignIn,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(10),
                ConsumedAtUtc = now // burned immediately — not verifiable
            });
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var mail = TransactionalEmails.EmailCodeUsePassword(baseUrl, culture);
                await _email.SendAsync(
                    new EmailMessage(email, mail.Subject, mail.Html, mail.Category),
                    cancellationToken);
            }
            catch
            {
                // Delivery must not change the identical 202 response.
            }

            return Accepted(dummy);
        }

        var purpose = user is null ? EmailSignInPurpose.SignUp : EmailSignInPurpose.SignIn;
        var code = VerificationCodes.CreateNumericCode();
        var challenge = new EmailSignInChallenge
        {
            Id = Guid.NewGuid(),
            EmailNormalized = email,
            CodeHash = VerificationCodes.Hash(code),
            Purpose = purpose,
            FirstName = firstName,
            ReferralCode = string.IsNullOrWhiteSpace(request.ReferralCode)
                ? null
                : request.ReferralCode.Trim(),
            ReturnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? null : request.ReturnUrl.Trim(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(10)
        };
        _db.EmailSignInChallenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var mail = purpose == EmailSignInPurpose.SignUp
                ? TransactionalEmails.EmailSignUpCode(baseUrl, code, culture)
                : TransactionalEmails.EmailSignInCode(baseUrl, code, culture);
            await _email.SendAsync(
                new EmailMessage(email, mail.Subject, mail.Html, mail.Category),
                cancellationToken);
        }
        catch
        {
            // Challenge stays; user can request a new code.
        }

        return Accepted(new EmailCodeStartResponse(challenge.Id));
    }

    /// <summary>Verifies a passwordless e-mail code and returns the local-login profile shape.</summary>
    [HttpPost("email-code/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LocalLoginResponse>> VerifyEmailCode(
        [FromBody] EmailCodeVerifyRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrustedProvisionCaller())
        {
            return Unauthorized(new { message = "Ongeldige provision-secret." });
        }

        var code = (request.Code ?? string.Empty).Trim();
        if (request.ChallengeId == Guid.Empty || code.Length != 6)
        {
            return StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" });
        }

        var challenge = await _db.EmailSignInChallenges
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, cancellationToken);
        var now = DateTime.UtcNow;
        if (challenge is null
            || challenge.ConsumedAtUtc is not null
            || challenge.ExpiresAtUtc <= now
            || challenge.FailedAttempts >= VerificationCodes.MaxFailedAttempts)
        {
            return StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" });
        }

        if (!VerificationCodes.MatchesHash(challenge.CodeHash, code))
        {
            var attempts = challenge.FailedAttempts;
            var burned = VerificationCodes.RegisterFailedAttempt(ref attempts);
            challenge.FailedAttempts = attempts;
            if (burned)
            {
                challenge.ConsumedAtUtc = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return burned
                ? StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" })
                : Unauthorized(new
                {
                    message = "invalid_code",
                    attemptsLeft = VerificationCodes.MaxFailedAttempts - challenge.FailedAttempts
                });
        }

        // Race: reload and consume only if still open (second parallel verify loses).
        await _db.Entry(challenge).ReloadAsync(cancellationToken);
        if (challenge.ConsumedAtUtc is not null || challenge.ExpiresAtUtc <= now)
        {
            return StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" });
        }

        challenge.ConsumedAtUtc = now;
        challenge.Version++;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" });
        }

        User user;
        if (challenge.Purpose == EmailSignInPurpose.SignUp)
        {
            var existing = await _db.Users
                .Include(u => u.CompanyMemberships)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == challenge.EmailNormalized, cancellationToken);
            if (existing is not null)
            {
                if (existing.Role != UserRole.Candidate || !existing.IsActive)
                {
                    return StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" });
                }

                user = existing;
            }
            else
            {
                var fullName = !string.IsNullOrWhiteSpace(challenge.FirstName)
                    ? challenge.FirstName.Trim()
                    : challenge.EmailNormalized.Split('@')[0];
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = challenge.EmailNormalized,
                    FullName = fullName,
                    FirstName = string.IsNullOrWhiteSpace(challenge.FirstName)
                        ? null
                        : challenge.FirstName.Trim(),
                    Role = UserRole.Candidate,
                    IsActive = true,
                    TermsAcceptedAt = now
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(challenge.ReferralCode))
                {
                    await _ambassadeurAttribution.TryAttributeCandidateAsync(
                        user.Id, challenge.ReferralCode, cancellationToken);
                }
            }
        }
        else
        {
            var existing = await _db.Users
                .Include(u => u.CompanyMemberships)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == challenge.EmailNormalized, cancellationToken);
            if (existing is null || !existing.IsActive || existing.Role != UserRole.Candidate)
            {
                return StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" });
            }

            user = existing;
        }

        if (user.AuthenticatorEnabled || MfaPolicy.IsRequired(user.Role))
        {
            return Ok(new LocalLoginResponse(
                user.Email,
                user.FullName,
                user.Role.ToString(),
                user.CompanyId,
                [],
                RequiresMfa: true,
                MfaEnrolled: user.AuthenticatorEnabled,
                MfaChallengeToken: _mfaChallenges.Create(user, request.RememberDevice, localPassword: true),
                UserId: user.Id));
        }

        var flags = await BuildFlagsAsync(user, cancellationToken);
        var sessionToken = CreateLocalSessionToken(user.Email, user.Id);
        Guid? deviceSessionId = null;
        string? deviceRefresh = null;
        DateTime? deviceExpires = null;
        if (request.RememberDevice)
        {
            var device = await _deviceSessions.CreateAsync(
                user.Id,
                request.UserAgent ?? Request.Headers.UserAgent.ToString(),
                cancellationToken);
            deviceSessionId = device.DeviceSessionId;
            deviceRefresh = device.RefreshToken;
            deviceExpires = device.ExpiresAtUtc;
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new LocalLoginResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            flags.CompanyIds,
            flags.ShowCandidateHowTo,
            flags.HasCandidateApplications,
            flags.HasSalesReferral,
            sessionToken,
            user.SessionVersion,
            deviceSessionId,
            deviceRefresh,
            deviceExpires,
            user.Id));
    }

    private string? CreateLocalSessionToken(string email, Guid userId)
    {
        var secret = JobsyLocalSessionToken.ResolveSigningKey(
            _configuration["JobsyAuth:LocalSessionSigningKey"],
            _configuration["JobsyAuth:DevelopmentAuthSecret"]);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        return JobsyLocalSessionToken.Create(email, userId, secret);
    }

    private static string? NormalizeExternalProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "entra" or "microsoft" or "microsoftentra" or "oidc" => "entra",
            "google" or "googleentra" => "google",
            var p => p
        };
    }

    private async Task EnsureExternalLoginBoundAsync(
        Guid userId,
        string provider,
        string subject,
        string emailAtLink,
        CancellationToken cancellationToken)
    {
        var existing = await _db.UserExternalLogins
            .FirstOrDefaultAsync(
                l => l.Provider == provider && l.ProviderSubject == subject,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.UserId != userId)
            {
                // Subject already bound to another account — do not steal the link.
                return;
            }

            return;
        }

        // One subject per provider+user (re-bind if the same user signs in again with a new OID — rare).
        var priorForUser = await _db.UserExternalLogins
            .Where(l => l.UserId == userId && l.Provider == provider)
            .ToListAsync(cancellationToken);
        if (priorForUser.Count > 0)
        {
            // Keep the first OID binding; ignore later subjects for the same provider.
            return;
        }

        _db.UserExternalLogins.Add(new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderSubject = subject,
            EmailAtLink = emailAtLink,
            LinkedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Public status for login buttons (no secrets). True when Integraties has Client ID + secret.
    /// </summary>
    [HttpGet("external-providers")]
    [AllowAnonymous]
    public async Task<ActionResult<ExternalProvidersStatusResponse>> GetExternalProviders(
        CancellationToken cancellationToken)
    {
        var entra = await _credentials.GetAsync(IntegrationKey.MicrosoftEntra, cancellationToken);
        var google = await _credentials.GetAsync(IntegrationKey.GoogleEntra, cancellationToken);
        return Ok(new ExternalProvidersStatusResponse(
            Entra: IsOAuthConfigured(entra),
            Google: IsOAuthConfigured(google)));
    }

    /// <summary>
    /// Server-to-server OAuth client config for Jobsy.Web (Integraties credentials).
    /// </summary>
    [HttpGet("external-provider-config/{provider}")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ExternalProviderConfigResponse>> GetExternalProviderConfig(
        string provider,
        CancellationToken cancellationToken)
    {
        if (!IsTrustedProvisionCaller())
        {
            return Unauthorized(new { message = "Ongeldige provision-secret." });
        }

        var key = provider.Trim().ToLowerInvariant() switch
        {
            "entra" or "microsoft" or "microsoftentra" => IntegrationKey.MicrosoftEntra,
            "google" or "googleentra" => IntegrationKey.GoogleEntra,
            _ => (IntegrationKey?)null
        };
        if (key is null)
        {
            return BadRequest(new { message = "Onbekende provider." });
        }

        var secrets = await _credentials.GetSecretsAsync(key.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(secrets?.ClientId)
            || string.IsNullOrWhiteSpace(secrets.ClientSecret))
        {
            return NotFound(new { message = "Provider is niet geconfigureerd in Integraties." });
        }

        return Ok(new ExternalProviderConfigResponse(
            key.Value.ToString(),
            secrets.ClientId.Trim(),
            secrets.ClientSecret.Trim(),
            string.IsNullOrWhiteSpace(secrets.TenantId) ? null : secrets.TenantId.Trim()));
    }

    private static bool IsOAuthConfigured(IntegrationCredentialView? view)
        => view is not null
           && !string.IsNullOrWhiteSpace(view.ClientId)
           && view.HasClientSecret;

    private bool IsTrustedProvisionCaller()
    {
        // Never accept DevelopmentAuthSecret here — that secret only unlocks demo header-auth.
        // OAuth client secrets require a dedicated ExternalProvisionSecret (or loopback in Development).
        var expected = _configuration["JobsyAuth:ExternalProvisionSecret"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            // Fail closed outside Development. Local DX may use loopback without a secret.
            if (!_environment.IsDevelopment())
            {
                return false;
            }

            var remote = HttpContext.Connection.RemoteIpAddress;
            return remote is null
                   || System.Net.IPAddress.IsLoopback(remote);
        }

        if (!Request.Headers.TryGetValue("X-Jobsy-Provision-Secret", out var provided))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        return expectedBytes.Length == providedBytes.Length
               && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private async Task<(IReadOnlyList<Guid> CompanyIds, bool ShowCandidateHowTo, bool HasCandidateApplications, bool HasSalesReferral)>
        BuildFlagsAsync(User user, CancellationToken cancellationToken)
    {
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

        // Eerste login ooit → Hoe werkt Lobsy; daarna → banenkaart (nav blijft beschikbaar).
        var isFirstLogin = user.LastLoginAtUtc is null;
        var showHowTo = user.Role == UserRole.Candidate && isFirstLogin;

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (companyIds, showHowTo, hasApps, hasSalesReferral);
    }
}
