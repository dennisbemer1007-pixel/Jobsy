using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Middleware;

/// <summary>
/// 301 redirects from legacy /employer|/branch|/regional URLs to /werkgever (GET/HEAD only).
/// /home → /werkgever only for authenticated employer roles.
/// </summary>
public sealed class WerkgeverLegacyRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public WerkgeverLegacyRedirectMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            var path = context.Request.Path.Value ?? "/";
            var query = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value
                : null;

            if (WerkgeverLegacyRoutes.IsUntouchedPaymentPath(path))
            {
                await _next(context);
                return;
            }

            if (IsEmployerHome(path) && IsEmployerUser(context.User))
            {
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = "/werkgever" + (query ?? "");
                return;
            }

            if (WerkgeverLegacyRoutes.TryMap(path, out var newPath, out var tab))
            {
                var target = WerkgeverLegacyRoutes.BuildTarget(newPath, query, tab);
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = target;
                return;
            }
        }

        await _next(context);
    }

    private static bool IsEmployerHome(string path)
    {
        var n = WerkgeverLegacyRoutes.Normalize(path);
        return n is "/home";
    }

    private static bool IsEmployerUser(ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true
           && RoleClaimMatching.HasAnyRole(user, JobsyRoles.EmployerRoles);
}
