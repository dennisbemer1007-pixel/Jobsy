using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jobsy.Web.Security;

/// <summary>301 legacy /salesmanager/* → /sales/* (query string preserved).</summary>
public sealed class SalesLegacyRoutesMiddleware
{
    private readonly RequestDelegate _next;

    public SalesLegacyRoutesMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (SalesLegacyRoutes.Map.TryGetValue(path, out var target)
            || SalesLegacyRoutes.Map.TryGetValue(path.TrimEnd('/'), out target))
        {
            var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
            if (target.Contains('?', StringComparison.Ordinal) && !string.IsNullOrEmpty(qs))
            {
                target += "&" + qs.TrimStart('?');
            }
            else
            {
                target += qs;
            }

            context.Response.Redirect(target, permanent: true);
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Returns 404 JSON feature_disabled for Ambassadeur pages/APIs while AmbassadorsEnabled is off.
/// Public landings /werven and /ambassadeur/ref redirect to / without cookie.
/// </summary>
public sealed class AmbassadorsFeatureMiddleware
{
    private readonly RequestDelegate _next;

    public AmbassadorsFeatureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var features = context.RequestServices.GetService<IPlatformFeatureService>();
        var enabled = features is null
            || await AmbassadorsFeatureGate.IsEnabledAsync(features, context.RequestAborted);

        if (!enabled)
        {
            if (IsPublicLanding(path))
            {
                context.Response.Cookies.Delete("lobsy_ambassadeur_ref");
                context.Response.Redirect("/", permanent: false);
                return;
            }

            if (IsGated(path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(AmbassadorsFeatureGate.FeatureDisabledJson);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true
                && context.User.IsInRole("Ambassadeur")
                && !context.User.IsInRole("Admin")
                && !context.User.IsInRole("SalesManager")
                && !HasEmployerRole(context.User))
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Response.Redirect("/login?error=ambassadors_parked");
                return;
            }
        }

        await _next(context);
    }

    private static bool IsPublicLanding(string path)
        => path.StartsWith("/werven/", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/ambassadeur/ref/", StringComparison.OrdinalIgnoreCase);

    private static bool IsGated(string path)
        => path.Equals("/ambassadeur", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/ambassadeur/", StringComparison.OrdinalIgnoreCase)
           || path.Equals("/admin/gebruikers/sales", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/admin/gebruikers/sales/", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/api/ambassadeurs", StringComparison.OrdinalIgnoreCase);

    private static bool HasEmployerRole(System.Security.Claims.ClaimsPrincipal user)
        => user.IsInRole("BranchManager")
           || user.IsInRole("RegionalManager")
           || user.IsInRole("EnterpriseManager")
           || user.IsInRole("Intermediary");
}
