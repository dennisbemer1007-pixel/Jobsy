using System.Security.Claims;
using System.Text.Encodings.Web;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Jobsy.Api.Authorization;

public static class AuthorizationExtensions
{
    public const string DevelopmentScheme = "JobsyDevelopment";

    public static IServiceCollection AddJobsyApiAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var azureAdSection = configuration.GetSection("AzureAd");
        var hasEntra = !string.IsNullOrWhiteSpace(azureAdSection["ClientId"]);
        var allowDevelopmentAuth = environment.IsDevelopment()
            || configuration.GetValue("JobsyAuth:AllowDevelopmentAuth", false);

        if (!allowDevelopmentAuth && !hasEntra)
        {
            throw new InvalidOperationException(
                "AzureAd:ClientId is required when JobsyAuth:AllowDevelopmentAuth is false. Header-based DevelopmentAuth is disabled.");
        }

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = hasEntra
                ? JwtBearerDefaults.AuthenticationScheme
                : DevelopmentScheme;
            options.DefaultChallengeScheme = hasEntra
                ? JwtBearerDefaults.AuthenticationScheme
                : DevelopmentScheme;
        });

        if (hasEntra)
        {
            authBuilder.AddMicrosoftIdentityWebApi(azureAdSection);
        }

        // Header auth for local Development and temporary cloud demos (explicit flag only).
        if (allowDevelopmentAuth)
        {
            authBuilder.AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>(
                DevelopmentScheme,
                _ => { });
        }

        services.AddAuthorization(options =>
        {
            // Fail closed: new endpoints require auth unless explicitly [AllowAnonymous].
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
        });

        services.AddScoped<CompanyScopeFilter>();

        return services;
    }
}

/// <summary>
/// Local/demo authentication via headers — Development or explicit JobsyAuth:AllowDevelopmentAuth.
/// Header <c>X-Jobsy-Email</c> must match an active DB user; role/company claims come from the database
/// (client-supplied <c>X-Jobsy-Role</c> is ignored for privilege).
/// </summary>
public sealed class DevelopmentAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly JobsyDbContext _db;

    public DevelopmentAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        JobsyDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Jobsy-Email", out var emailValues))
        {
            return AuthenticateResult.NoResult();
        }

        var email = emailValues.FirstOrDefault()?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthenticateResult.NoResult();
        }

        var dbUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        if (dbUser is null)
        {
            return AuthenticateResult.Fail("Unknown or inactive user for Development auth.");
        }

        var name = Request.Headers["X-Jobsy-Name"].FirstOrDefault() ?? dbUser.FullName;
        var role = dbUser.Role.ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
            new(ClaimTypes.Email, dbUser.Email),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        };

        if (dbUser.CompanyId is Guid primaryCompany)
        {
            claims.Add(new Claim(JobsyClaimTypes.CompanyId, primaryCompany.ToString()));
        }

        var membershipIds = dbUser.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
        if (membershipIds.Count > 0)
        {
            claims.Add(new Claim(JobsyClaimTypes.CompanyIds, string.Join(',', membershipIds)));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
