using System.Text.RegularExpressions;

namespace Jobsy.Web.Middleware;

/// <summary>
/// Keeps public URL <c>/after-match</c> while routing pick-slot and confirmed interview to SSR and other panels to Blazor.
/// </summary>
public sealed partial class AfterMatchPickSlotRouteRewriteMiddleware
{
    private readonly RequestDelegate _next;

    public AfterMatchPickSlotRouteRewriteMiddleware(RequestDelegate next)
        => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.Value ?? "";
            var match = AfterMatchBasePath().Match(path);
            if (match.Success)
            {
                var panel = context.Request.Query["panel"].ToString().Trim().ToLowerInvariant();
                var suffix = panel switch
                {
                    "pick-slot" or "propose-slot" => "/pick-slot-ssr",
                    "messages" => "/messages-ssr",
                    "reschedule" or "cancel" => "/reschedule-ssr",
                    "" => "/confirmed-ssr",
                    _ => "/blazor",
                };
                context.Request.Path = new PathString(match.Groups[1].Value + suffix);
            }
        }

        return _next(context);
    }

    [GeneratedRegex(
        @"^(?<prefix>/candidate/applications/[0-9a-fA-F-]{36}/after-match)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex AfterMatchBasePath();
}

public static class AfterMatchPickSlotRouteRewriteMiddlewareExtensions
{
    public static IApplicationBuilder UseAfterMatchPickSlotRouteRewrite(this IApplicationBuilder app)
        => app.UseMiddleware<AfterMatchPickSlotRouteRewriteMiddleware>();
}
