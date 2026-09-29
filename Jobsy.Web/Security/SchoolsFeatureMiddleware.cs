using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;

namespace Jobsy.Web.Security;

/// <summary>
/// Returns 404 JSON <c>feature_disabled</c> for /school*, /leraar*, /leerling* and matching APIs
/// when <see cref="PlatformFeatureSnapshot.SchoolsEnabled"/> is false. Admin routes stay open.
/// </summary>
public sealed class SchoolsFeatureMiddleware
{
    private readonly RequestDelegate _next;

    public SchoolsFeatureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IPlatformFeatureService features)
    {
        var path = context.Request.Path.Value ?? "";
        if (IsGated(path) && !await SchoolsFeatureGate.IsEnabledAsync(features, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"error":"feature_disabled"}""");
            return;
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
