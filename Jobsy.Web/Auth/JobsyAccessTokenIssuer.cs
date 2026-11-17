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
        _privatePem = JobsyJwtPemBootstrap.ResolvePrivatePem(configuration, environment);
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
            clientIp: clientIp,
            mfaVerified: user.HasClaim(JobsyClaimTypes.MfaVerified, "1"),
            authMethod: user.FindFirst("auth_method")?.Value);
    }
}
