namespace Jobsy.Web.Security;

/// <summary>Permanent redirect for removed /register/activate (auth 06).</summary>
public sealed class LegacyAuthRouteRedirects
{
    private readonly RequestDelegate _next;

    public LegacyAuthRouteRedirects(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.Equals("/register/activate", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = "/register";
            return;
        }

        await _next(context);
    }
}

public static class LegacyAuthRouteRedirectsExtensions
{
    public static IApplicationBuilder UseLegacyAuthRouteRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<LegacyAuthRouteRedirects>();
}
