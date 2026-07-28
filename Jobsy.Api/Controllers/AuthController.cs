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
    private readonly IHostEnvironment _environment;

    public AuthController(
        JobsyDbContext db,
        IConfiguration configuration,
        IIntegrationCredentialService credentials,
        IHostEnvironment environment)
    {
        _db = db;
        _configuration = configuration;
        _credentials = credentials;
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
            flags.HasCandidateApplications));
    }

    /// <summary>
    /// Upserts an external (Google/Entra) identity into Jobsy Users.
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

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        var isNew = false;
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
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var flags = await BuildFlagsAsync(user, cancellationToken);
        return Ok(new EnsureExternalUserResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            flags.CompanyIds,
            isNew,
            flags.ShowCandidateHowTo,
            flags.HasCandidateApplications));
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
        var expected = _configuration["JobsyAuth:DevelopmentAuthSecret"]
                       ?? _configuration["JobsyAuth:ExternalProvisionSecret"];
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

    private async Task<(IReadOnlyList<Guid> CompanyIds, bool ShowCandidateHowTo, bool HasCandidateApplications)>
        BuildFlagsAsync(User user, CancellationToken cancellationToken)
    {
        var companyIds = user.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
        if (user.CompanyId is Guid primary && !companyIds.Contains(primary))
        {
            companyIds.Insert(0, primary);
        }

        var hasApps = await _db.Applications.AsNoTracking()
            .AnyAsync(a => a.CandidateUserId == user.Id, cancellationToken);

        // Eerste login ooit → Hoe werkt Lobsy; daarna → banenkaart (nav blijft beschikbaar).
        var isFirstLogin = user.LastLoginAtUtc is null;
        var showHowTo = user.Role == UserRole.Candidate && isFirstLogin;

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (companyIds, showHowTo, hasApps);
    }
}
