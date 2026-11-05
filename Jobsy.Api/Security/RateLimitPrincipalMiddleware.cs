using System.Security.Claims;
using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Api.Security;

/// <summary>
/// Ensures rate limiters see the same principals as endpoint authorization: JWT Bearer,
/// pupil cookie (<c>/api/pupil/*</c>), and parent passport cookie (<c>/api/parent/*</c>).
/// Runs after <see cref="AuthenticationMiddleware"/> and before <c>UseRateLimiter</c>.
/// </summary>
public sealed class RateLimitPrincipalMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitPrincipalMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await TryAuthenticateJwtAsync(context);

        var path = context.Request.Path;
        if (path.StartsWithSegments("/api/pupil", StringComparison.OrdinalIgnoreCase))
        {
            await TryMergeSchemeAsync(context, PupilAuthDefaults.Scheme);
        }
        else if (path.StartsWithSegments("/api/parent", StringComparison.OrdinalIgnoreCase))
        {
            await TryMergeSchemeAsync(context, ParentPassportAuthDefaults.Scheme);
        }

        await _next(context);
    }

    private static async Task TryAuthenticateJwtAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader)
            || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var jwt = await context.AuthenticateAsync(AuthorizationExtensions.JobsyJwtScheme);
        if (jwt.Succeeded && jwt.Principal?.Identity?.IsAuthenticated == true)
        {
            MergePrincipal(context, jwt.Principal);
        }
    }

    private static async Task TryMergeSchemeAsync(HttpContext context, string scheme)
    {
        var auth = await context.AuthenticateAsync(scheme);
        if (auth.Succeeded
            && auth.Principal?.Identity?.IsAuthenticated == true)
        {
            MergePrincipal(context, auth.Principal);
        }
    }

    private static void MergePrincipal(HttpContext context, ClaimsPrincipal additional)
    {
        var identities = context.User.Identities.ToList();
        if (additional.Identity is ClaimsIdentity identity)
        {
            identities.Add(identity);
        }
        else
        {
            identities.Add(new ClaimsIdentity(additional.Claims, additional.Identity?.AuthenticationType));
        }

        context.User = new ClaimsPrincipal(identities);
    }
}
