using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Jobsy.Core.Authorization;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace Jobsy.Api.Authorization;

public static class AuthorizationExtensions
{
    public const string JobsyJwtScheme = "JobsyJwt";

    public static IServiceCollection AddJobsyApiAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var azureAdSection = configuration.GetSection("AzureAd");
        var hasEntra = !string.IsNullOrWhiteSpace(azureAdSection["ClientId"]);

        var publicPem = JobsyAccessToken.NormalizePem(configuration["JobsyAuth:Jwt:PublicKeyPem"]);
        var privatePem = JobsyAccessToken.NormalizePem(configuration["JobsyAuth:Jwt:PrivateKeyPem"]);
        if (string.IsNullOrWhiteSpace(publicPem) || !publicPem.Contains("BEGIN", StringComparison.Ordinal))
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "JobsyAuth:Jwt:PublicKeyPem is required in Production. " +
                    "Set JobsyAuth__Jwt__PublicKeyPem to the ES256 public key (PEM).");
            }

            publicPem = JobsyAccessToken.DevelopmentPublicKeyPem;
            privatePem = JobsyAccessToken.DevelopmentPrivateKeyPem;
        }

        JobsyDevJwtKeys.PrivatePem = string.IsNullOrWhiteSpace(privatePem)
            ? JobsyAccessToken.DevelopmentPrivateKeyPem
            : privatePem;
        JobsyDevJwtKeys.PublicPem = publicPem;

        var issuer = configuration["JobsyAuth:Jwt:Issuer"] ?? JobsyAccessToken.DefaultIssuer;
        var audience = configuration["JobsyAuth:Jwt:Audience"] ?? JobsyAccessToken.DefaultAudience;
        var validationParams = JobsyAccessToken.CreateValidationParameters(publicPem, issuer, audience);

        services.AddSingleton(validationParams);
        services.AddSingleton<ISessionVersionGate, SessionVersionGate>();

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JobsyJwtScheme;
            options.DefaultChallengeScheme = JobsyJwtScheme;
        });

        authBuilder.AddJwtBearer(JobsyJwtScheme, options =>
        {
            options.TokenValidationParameters = validationParams;
            options.MapInboundClaims = false;
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var gate = context.HttpContext.RequestServices.GetRequiredService<ISessionVersionGate>();
                    var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                              ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var svRaw = context.Principal?.FindFirst(JobsyAccessToken.SessionVersionClaim)?.Value;
                    if (!Guid.TryParse(sub, out var userId)
                        || !int.TryParse(svRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sv))
                    {
                        context.Fail("Ongeldig toegangstoken.");
                        return;
                    }

                    var ok = await gate.IsValidAsync(userId, sv, context.HttpContext.RequestAborted);
                    if (!ok)
                    {
                        context.Fail("Sessie is verlopen of ingetrokken.");
                        return;
                    }

                    // Enrich principal from DB (roles/companies) — ignore any client identity headers.
                    var db = context.HttpContext.RequestServices.GetRequiredService<JobsyDbContext>();
                    var dbUser = await db.Users.AsNoTracking()
                        .Include(u => u.CompanyMemberships)
                        .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, context.HttpContext.RequestAborted);
                    if (dbUser is null)
                    {
                        context.Fail("Onbekende of inactieve gebruiker.");
                        return;
                    }

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, dbUser.Id.ToString("D")),
                        new(ClaimTypes.Email, dbUser.Email),
                        new(ClaimTypes.Name, dbUser.FullName),
                        new(ClaimTypes.Role, dbUser.Role.ToString()),
                        new(JobsyClaimTypes.SessionVersion, dbUser.SessionVersion.ToString(CultureInfo.InvariantCulture)),
                        new(JobsyAccessToken.SessionVersionClaim, dbUser.SessionVersion.ToString(CultureInfo.InvariantCulture))
                    };
                    if (dbUser.CompanyId is Guid primaryCompany)
                    {
                        claims.Add(new Claim(JobsyClaimTypes.CompanyId, primaryCompany.ToString("D")));
                    }

                    var membershipIds = dbUser.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
                    if (membershipIds.Count > 0)
                    {
                        claims.Add(new Claim(JobsyClaimTypes.CompanyIds, string.Join(',', membershipIds)));
                    }

                    var clientIp = context.Principal?.FindFirst(JobsyAccessToken.ClientIpClaim)?.Value;
                    if (!string.IsNullOrWhiteSpace(clientIp))
                    {
                        claims.Add(new Claim(JobsyAccessToken.ClientIpClaim, clientIp));
                    }

                    context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, JobsyJwtScheme));
                }
            };
        });

        if (hasEntra)
        {
            authBuilder.AddMicrosoftIdentityWebApi(azureAdSection);
        }

        authBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthDefaults.AuthenticationScheme,
            _ => { });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(JobsyPolicies.RequireAdmin, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(JobsyRoles.Admin));

            options.AddPolicy(JobsyPolicies.RequireEmployer, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(JobsyRoles.EmployerRoles));

            options.AddPolicy(JobsyPolicies.RequireAdminOrEmployer, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole([JobsyRoles.Admin, ..JobsyRoles.EmployerRoles]));

            options.AddPolicy(JobsyPolicies.RequireCandidate, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(JobsyRoles.Candidate));

            options.AddPolicy(JobsyPolicies.RequireSalesManager, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(JobsyRoles.SalesManager));

            options.AddPolicy(JobsyPolicies.RequireAdminOrSalesManager, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(JobsyRoles.Admin, JobsyRoles.SalesManager));

            options.AddPolicy(JobsyPolicies.RequireAmbassadeur, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(JobsyRoles.Ambassadeur));

            options.AddPolicy(JobsyPolicies.RequireAdminOrAmbassadeur, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(JobsyRoles.Admin, JobsyRoles.Ambassadeur));

            options.AddPolicy(JobsyPolicies.RequireDashboardAccess, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(
                        JobsyRoles.Admin,
                        JobsyRoles.BranchManager,
                        JobsyRoles.RegionalManager,
                        JobsyRoles.EnterpriseManager,
                        JobsyRoles.Intermediary,
                        JobsyRoles.SalesManager,
                        JobsyRoles.Ambassadeur));

            options.AddPolicy(JobsyPolicies.RequireApiKey, policy =>
                policy.AddAuthenticationSchemes(ApiKeyAuthDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser());
        });

        services.AddScoped<CompanyScopeFilter>();

        return services;
    }
}

/// <summary>Process-local ephemeral ES256 keys for Development when PEM env vars are unset.</summary>
public static class JobsyDevJwtKeys
{
    public static string? PrivatePem { get; set; }
    public static string? PublicPem { get; set; }
}

public interface ISessionVersionGate
{
    Task<bool> IsValidAsync(Guid userId, int sessionVersion, CancellationToken cancellationToken);
}

public sealed class SessionVersionGate : ISessionVersionGate
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public SessionVersionGate(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<bool> IsValidAsync(Guid userId, int sessionVersion, CancellationToken cancellationToken)
    {
        var cacheKey = "sv:" + userId.ToString("D");
        if (_cache.TryGetValue(cacheKey, out (int Version, int Minimum) snap)
            && sessionVersion == snap.Version
            && sessionVersion >= snap.Minimum)
        {
            return true;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new { u.SessionVersion })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            _cache.Remove(cacheKey);
            return false;
        }

        var features = await db.PlatformFeatureSettings.AsNoTracking()
            .OrderBy(f => f.Id)
            .Select(f => f.MinimumSessionVersion)
            .FirstOrDefaultAsync(cancellationToken);
        snap = (user.SessionVersion, features);
        _cache.Set(cacheKey, snap, TimeSpan.FromSeconds(60));
        return sessionVersion >= snap.Minimum && sessionVersion == snap.Version;
    }
}
