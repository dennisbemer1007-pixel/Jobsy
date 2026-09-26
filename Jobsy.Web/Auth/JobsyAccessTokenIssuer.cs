using System.Globalization;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Security;

namespace Jobsy.Web.Auth;

/// <summary>Mints short-lived ES256 access tokens for Web → API calls.</summary>
public sealed class JobsyAccessTokenIssuer
{
    private readonly string _privatePem;
    private readonly string _issuer;
    private readonly string _audience;

    public JobsyAccessTokenIssuer(IConfiguration configuration, IHostEnvironment environment)
    {
        var privatePem = JobsyAccessToken.NormalizePem(configuration["JobsyAuth:Jwt:PrivateKeyPem"]);
        if (string.IsNullOrWhiteSpace(privatePem) || !privatePem.Contains("BEGIN", StringComparison.Ordinal))
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "JobsyAuth:Jwt:PrivateKeyPem is verplicht in Production. " +
                    "Zet JobsyAuth__Jwt__PrivateKeyPem op de ES256 private key (PEM).");
            }

            privatePem = JobsyAccessToken.DevelopmentPrivateKeyPem;
        }

        _privatePem = privatePem;
        _issuer = configuration["JobsyAuth:Jwt:Issuer"] ?? JobsyAccessToken.DefaultIssuer;
        _audience = configuration["JobsyAuth:Jwt:Audience"] ?? JobsyAccessToken.DefaultAudience;
    }

    public string? TryCreate(ClaimsPrincipal user, string? clientIp = null)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var idRaw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idRaw, out var userId))
        {
            return null;
        }

        var svRaw = user.FindFirst(JobsyClaimTypes.SessionVersion)?.Value
                    ?? user.FindFirst(JobsyAccessToken.SessionVersionClaim)?.Value
                    ?? "0";
        if (!int.TryParse(svRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sv))
        {
            sv = 0;
        }

        return JobsyAccessToken.Create(
            userId,
            sv,
            _privatePem,
            _issuer,
            _audience,
            clientIp: clientIp);
    }
}
