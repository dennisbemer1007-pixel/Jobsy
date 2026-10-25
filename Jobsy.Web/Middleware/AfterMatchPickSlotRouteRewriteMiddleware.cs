using System.Text.RegularExpressions;
using Jobsy.Web.Applications;

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
                var prefix = match.Groups[1].Value;
                var panel = AfterMatchPanelRouting.ResolvePanel(context.Request.Query);
                var suffix = prefix.Contains("/werkgever/", StringComparison.OrdinalIgnoreCase)
                    ? AfterMatchEmployerPanelRouting.PathSuffixForPanel(panel)
                    : AfterMatchPanelRouting.PathSuffixForPanel(panel);
                context.Request.Path = new PathString(prefix + suffix);
            }
        }

        return _next(context);
    }

    [GeneratedRegex(
        @"^(?<prefix>/(?:candidate/applications|werkgever/sollicitaties)/[0-9a-fA-F-]{36}/after-match)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex AfterMatchBasePath();
}

public static class AfterMatchPickSlotRouteRewriteMiddlewareExtensions
{
    public static IApplicationBuilder UseAfterMatchPickSlotRouteRewrite(this IApplicationBuilder app)
        => app.UseMiddleware<AfterMatchPickSlotRouteRewriteMiddleware>();
}
