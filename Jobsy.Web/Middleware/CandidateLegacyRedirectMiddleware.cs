using Jobsy.Web.Navigation;

namespace Jobsy.Web.Middleware;

/// <summary>Redirects Dutch candidate URL aliases (e.g. /candidate/instellingen) to canonical paths.</summary>
public sealed class CandidateLegacyRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public CandidateLegacyRedirectMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.Value ?? "";
            if (CandidateLegacyRoutes.TryMap(path, out var mapped))
            {
                var target = mapped;
                if (context.Request.QueryString.HasValue)
                {
                    target += context.Request.QueryString.Value;
                }

                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = target;
                return;
            }
        }

        await _next(context);
    }
}

public static class CandidateLegacyRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseCandidateLegacyRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<CandidateLegacyRedirectMiddleware>();
}
