using Jobsy.Core.Features;
using Jobsy.Core.Scholen;

namespace Jobsy.Web.Security;

/// <summary>
/// When schools are off, /api/school|teacher|pupil stays a 404 JSON <c>feature_disabled</c>.
/// Browser requests to /school*, /leraar* and /leerling* go to the friendly access-denied page
/// (layout + logout) so school staff are not stuck on raw JSON. Admin routes stay open.
/// Runs before authorization so a teacher and a school admin get the same page.
/// </summary>
public sealed class SchoolsFeatureMiddleware
{
    private readonly RequestDelegate _next;

    public SchoolsFeatureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (IsGated(path))
        {
            var features = context.RequestServices.GetService<IFeatureFlags>();
            // Fail closed: a missing flag service must not open the portal.
            var enabled = features is not null
                && await SchoolsFeatureGate.IsEnabledAsync(features, context.RequestAborted);
            if (!enabled)
            {
                if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("""{"error":"feature_disabled"}""");
                    return;
                }

                context.Response.Headers.CacheControl = "no-store";
                context.Response.Redirect(FeatureRoutes.SchoolsOffAccessDeniedPath);
                return;
            }
        }

        await _next(context);
    }

    private static bool IsGated(string path)
    {
        return path.StartsWith("/school", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/leraar", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/leerling", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/api/school", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/api/teacher", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/api/pupil", StringComparison.OrdinalIgnoreCase);
    }
}
