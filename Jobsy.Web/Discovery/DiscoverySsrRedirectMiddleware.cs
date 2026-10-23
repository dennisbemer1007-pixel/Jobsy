using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Discovery;

/// <summary>
/// GET redirects for De ontdekkingsreis before Razor renders (303, no intermediate body).
/// </summary>
public sealed class DiscoverySsrRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (!path.Equals(DiscoverySsrUrls.Path, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true
            || !RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            await next(context);
            return;
        }

        var api = context.RequestServices.GetRequiredService<JobsyApiClient>();
        var featureFlags = context.RequestServices.GetRequiredService<IFeatureFlags>();
        var culture = context.RequestServices.GetRequiredService<CultureState>();
        var overviewCache = context.RequestServices.GetRequiredService<DiscoveryOverviewCache>();

        var bootstrap = await DiscoverySsrData.LoadBootstrapAsync(api, context.RequestAborted);
        if (bootstrap is null)
        {
            await next(context);
            return;
        }

        var query = QueryHelpers.ParseQuery(context.Request.QueryString.Value ?? "");
        query.TryGetValue("stap", out var stapValues);
        query.TryGetValue("fase", out var faseValues);
        query.TryGetValue("diepte", out var diepteValues);

        var redirectUrl = await DiscoverySsrRedirect.TryGetRedirectUrlAsync(
            context.Request,
            api,
            featureFlags,
            culture,
            overviewCache,
            bootstrap.State,
            bootstrap.Draft,
            stapValues.FirstOrDefault(),
            faseValues.FirstOrDefault(),
            diepteValues.FirstOrDefault(),
            context.RequestAborted);

        if (string.IsNullOrEmpty(redirectUrl)
            || DiscoverySsrRedirect.IsSameLocation(context.Request, redirectUrl))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status303SeeOther;
        context.Response.Headers.Location = redirectUrl;
        context.Response.Headers.CacheControl = "no-store";
    }
}

public static class DiscoverySsrRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseDiscoverySsrRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<DiscoverySsrRedirectMiddleware>();
}
