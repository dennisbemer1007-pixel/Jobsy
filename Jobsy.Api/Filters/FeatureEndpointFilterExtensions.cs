using Jobsy.Core.Features;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Api.Filters;

/// <summary>Minimal-API endpoint filter that returns 404 when a feature is disabled.</summary>
public static class FeatureEndpointFilterExtensions
{
    public static RouteHandlerBuilder RequireFeature(
        this RouteHandlerBuilder builder,
        PlatformFeature feature,
        bool whenEnabled = true)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var flags = context.HttpContext.RequestServices.GetRequiredService<IFeatureFlags>();
            var snap = await flags.GetAsync(context.HttpContext.RequestAborted);
            var enabled = snap.IsEnabled(feature);
            var ok = whenEnabled ? enabled : !enabled;
            if (!ok)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: $"Feature '{feature}' is not available.",
                    type: FeatureGateFilter.FeatureDisabledType);
            }

            return await next(context);
        });
    }
}
