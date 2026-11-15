using Jobsy.Core.Features;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Api.Security;

public sealed class WerkgeverApkFeatureMiddleware
{
    private readonly RequestDelegate _next;

    public WerkgeverApkFeatureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/api/werkgever-apk", StringComparison.OrdinalIgnoreCase))
        {
            var features = context.RequestServices.GetService(typeof(IFeatureFlags)) as IFeatureFlags;
            if (features is null
                || !await WerkgeverApkFeatureGate.IsGlobalEnabledAsync(features, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"error":"feature_disabled"}""");
                return;
            }
        }

        await _next(context);
    }
}
