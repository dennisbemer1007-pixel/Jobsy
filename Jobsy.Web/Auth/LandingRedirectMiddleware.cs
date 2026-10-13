using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Web.Auth;

/// <summary>
/// Runs before Blazor for path "/": signed-in → role home, legacy map deep links → /banenkaart,
/// otherwise the landing page with §S cache / variant headers.
/// </summary>
public sealed class LandingRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsLandingPath(context.Request.Path)
            || !(HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var home = await HomeForAsync(context);
            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.Location = home;
            return;
        }

        if (LegacyMapQuery.IsMapDeepLink(context.Request.Query))
        {
            var location = PublicRoutes.Banenkaart + context.Request.QueryString.Value;
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = location;
            return;
        }

        // Anonymous landing: set §S headers, then render the page.
        var employers = context.RequestServices.GetService<IEmployersSwitch>();
        var variant = LandingVariant.On;
        if (employers is not null)
        {
            variant = await employers.VariantAsync(context.RequestAborted);
        }

        // Development ?_variant= override (LandingVariantResolver also reads this).
        if (string.Equals(
                context.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName,
                Environments.Development,
                StringComparison.OrdinalIgnoreCase)
            && LandingVariantResolver.TryParse(context.Request.Query["_variant"], out var overrideVariant))
        {
            variant = overrideVariant;
        }

        context.Items[LandingVariantResolver.HttpContextItemsKey] = variant;
        context.Response.Headers.CacheControl = "no-cache, private";
        context.Response.Headers.Append("Vary", "Cookie");
        context.Response.Headers["X-Lobsy-Variant"] = variant == LandingVariant.Zw ? "zw" : "on";

        await next(context);
    }

    private static bool IsLandingPath(PathString path)
        => path.HasValue
           && (string.Equals(path.Value, "/", StringComparison.Ordinal)
               || string.Equals(path.Value, "", StringComparison.Ordinal));

    /// <summary>
    /// Candidate → <see cref="FeatureRoutes.HomeFor"/> (paspoort ON) or banenkaart (paspoort OFF).
    /// Everyone else → /home.
    /// </summary>
    public static async Task<string> HomeForAsync(HttpContext context)
    {
        var user = context.User;
        if (!RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return AuthRedirects.PostLoginUrl("/");
        }

        var flagsSvc = context.RequestServices.GetService<IFeatureFlags>();
        var flags = flagsSvc is null
            ? FeatureFlagSnapshot.Defaults
            : await flagsSvc.GetAsync(context.RequestAborted);

        if (!flags.CandidatePassportEnabled)
        {
            // Flag OFF: keep today's logo/landing redirect to the banenkaart.
            return AuthRedirects.BanenkaartPath;
        }

        return FeatureRoutes.HomeFor(
            user,
            flags,
            passportReady: AuthRedirects.PassportReadyFromClaims(user));
    }

    /// <summary>
    /// Sync helper for unit tests (passport OFF / defaults). Prefer <see cref="HomeForAsync"/>.
    /// </summary>
    public static string HomeFor(ClaimsPrincipal user)
    {
        if (user.IsInRole(JobsyRoles.Candidate))
        {
            return AuthRedirects.BanenkaartPath;
        }

        return AuthRedirects.PostLoginUrl("/");
    }
}

public static class LandingRedirectExtensions
{
    public static IApplicationBuilder UseLandingRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<LandingRedirectMiddleware>();
}
