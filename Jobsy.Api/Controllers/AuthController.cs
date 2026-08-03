using System.Security.Cryptography;
using System.Text;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
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

    public AuthController(
        JobsyDbContext db,
        IConfiguration configuration,
        IIntegrationCredentialService credentials,
        IAmbassadeurAttributionService ambassadeurAttribution,
        IHostEnvironment environment)
    {
        _db = db;
        _configuration = configuration;
        _credentials = credentials;
        _ambassadeurAttribution = ambassadeurAttribution;
        _environment = environment;
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

        var email = request.Email.Trim().ToLowerInvariant();
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        if (credential is null
            || !JobsyPasswordHasher.Verify(request.Password, credential.PasswordHash))
        {
            return Unauthorized(new { message = "Ongeldige e-mail of wachtwoord." });
        }

        if (JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(request.Password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Id == credential.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Account is niet actief." });
        }

        var flags = await BuildFlagsAsync(user, cancellationToken);
        return Ok(new LocalLoginResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            flags.CompanyIds,
            flags.ShowCandidateHowTo,
            flags.HasCandidateApplications,
            flags.HasSalesReferral));
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
                IsActive = true
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
        return Ok(new EnsureExternalUserResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            flags.CompanyIds,
            isNew,
            flags.ShowCandidateHowTo,
            flags.HasCandidateApplications,
            flags.HasSalesReferral));
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
