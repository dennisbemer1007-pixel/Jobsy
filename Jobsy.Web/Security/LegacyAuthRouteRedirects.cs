namespace Jobsy.Web.Security;

/// <summary>Temporary redirects for legacy auth URLs (token-less activate).</summary>
public sealed class LegacyAuthRouteRedirects
{
    private readonly RequestDelegate _next;

    public LegacyAuthRouteRedirects(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.Equals("/register/activate", StringComparison.OrdinalIgnoreCase))
        {
            var token = context.Request.Query["token"].ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                context.Response.Redirect("/register");
                return;
            }
        }

        await _next(context);
    }
}

public static class LegacyAuthRouteRedirectsExtensions
{
    public static IApplicationBuilder UseLegacyAuthRouteRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<LegacyAuthRouteRedirects>();
}
