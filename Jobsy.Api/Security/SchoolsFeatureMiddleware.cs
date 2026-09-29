using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;

namespace Jobsy.Api.Security;

/// <summary>
/// Returns 404 JSON <c>feature_disabled</c> for school/teacher/pupil APIs when SchoolsEnabled is false.
/// Admin school APIs stay reachable.
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
            var features = context.RequestServices.GetService(typeof(IPlatformFeatureService))
                as IPlatformFeatureService;
            if (features is not null
                && !await SchoolsFeatureGate.IsEnabledAsync(features, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"error":"feature_disabled"}""");
                return;
            }
        }

        await _next(context);
    }

    private static bool IsGated(string path)
        => path.StartsWith("/api/school", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/api/teacher", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/api/pupil", StringComparison.OrdinalIgnoreCase);
}
