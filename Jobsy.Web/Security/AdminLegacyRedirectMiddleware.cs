using Jobsy.Web.Navigation;

namespace Jobsy.Web.Security;

/// <summary>
/// 301 redirects from legacy English admin URLs to the Dutch §IA paths (D5).
/// Runs before endpoint routing; GET/HEAD only. Query string is preserved; optional tab= is merged.
/// </summary>
public sealed class AdminLegacyRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public AdminLegacyRedirectMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.Value ?? "";
            var query = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value!.TrimStart('?')
                : "";
            var relative = string.IsNullOrEmpty(query) ? path : path + "?" + query;

            if (AdminLegacyRoutes.TryMap(relative, out var destination))
            {
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = destination;
                return;
            }
        }

        await _next(context);
    }
}

public static class AdminLegacyRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminLegacyRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<AdminLegacyRedirectMiddleware>();
}
