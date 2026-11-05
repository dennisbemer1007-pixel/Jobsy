using System.Security.Claims;
using Jobsy.Core.Security;
using Jobsy.Web.Security;

namespace Jobsy.Web.Auth;

/// <summary>Attaches the trusted visitor IP to anonymous Web→API auth calls.</summary>
public sealed class TrustedClientIpHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public TrustedClientIpHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ApplyTrustedClientIp(request, _httpContextAccessor.HttpContext, _configuration);
        return base.SendAsync(request, cancellationToken);
    }

    public static void ApplyTrustedClientIp(
        HttpRequestMessage request,
        HttpContext? httpContext,
        IConfiguration configuration)
    {
        ApplyTrustedClientIp(request, httpContext, user: null, configuration);
    }

    public static void ApplyTrustedClientIp(
        HttpRequestMessage request,
        HttpContext? httpContext,
        ClaimsPrincipal? user,
        IConfiguration configuration)
    {
        request.Headers.Remove(InternalClientIpHeaders.ClientIpHeader);
        request.Headers.Remove(InternalClientIpHeaders.InternalSecretHeader);
        request.Headers.Remove(InternalClientIpHeaders.RateLimitUserIdHeader);

        var secret = configuration[InternalClientIpHeaders.ConfigKey];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return;
        }

        var clientIp = TrustedClientIp.Resolve(httpContext);
        if (string.IsNullOrWhiteSpace(clientIp))
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(InternalClientIpHeaders.ClientIpHeader, clientIp);
        request.Headers.TryAddWithoutValidation(InternalClientIpHeaders.InternalSecretHeader, secret.Trim());

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst("sub")?.Value
                         ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userId, out _))
            {
                request.Headers.TryAddWithoutValidation(
                    InternalClientIpHeaders.RateLimitUserIdHeader,
                    userId);
            }
        }
    }
}
