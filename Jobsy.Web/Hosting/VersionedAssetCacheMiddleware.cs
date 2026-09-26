namespace Jobsy.Web.Hosting;

/// <summary>
/// Blazor framework files under <c>/_framework</c> are not served via
/// <see cref="StaticFileOptions"/>, so Cloudflare saw them as DYNAMIC / no-cache.
/// Versioned URLs (<c>?v=</c>) get a long immutable Cache-Control.
/// </summary>
public sealed class VersionedAssetCacheMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase)
            && context.Request.Query.ContainsKey("v"))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl =
                    WebPerformanceExtensions.StaticAssetCacheControl(versioned: true);
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
