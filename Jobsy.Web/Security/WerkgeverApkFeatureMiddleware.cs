using Jobsy.Core.Features;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Security;

public sealed class WerkgeverApkFeatureMiddleware
{
    private readonly RequestDelegate _next;

    public WerkgeverApkFeatureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (!IsGated(path))
        {
            await _next(context);
            return;
        }

        var features = context.RequestServices.GetService<IFeatureFlags>();
        if (features is null
            || !await WerkgeverApkFeatureGate.IsGlobalEnabledAsync(features, context.RequestAborted))
        {
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"error":"feature_disabled"}""");
                return;
            }

            context.Response.Headers.CacheControl = "no-store";
            context.Response.Redirect("/werkgever?apk=off");
            return;
        }

        await _next(context);
    }

    private static bool IsGated(string path)
        => path.StartsWith("/loopbaan-apk", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/werkgever/loopbaan-apk", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/api/werkgever-apk", StringComparison.OrdinalIgnoreCase);

}