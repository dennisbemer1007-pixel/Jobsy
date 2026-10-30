using Jobsy.Core.Features;
using Microsoft.AspNetCore.Http.Extensions;

namespace Jobsy.Web.Middleware;

/// <summary>
/// <c>/bedrijvenkaart?ssr=1</c> → preview SSR path while production route stays on Blazor.
/// </summary>
public sealed class BedrijvenkaartSsrPreviewMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (BedrijvenkaartSsrPreview.IsEnabled(context.RequestServices.GetRequiredService<IConfiguration>())
            && HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.Equals("/bedrijvenkaart", StringComparison.OrdinalIgnoreCase)
            && context.Request.Query.TryGetValue("ssr", out var ssr)
            && string.Equals(ssr.ToString(), "1", StringComparison.Ordinal))
        {
            var qb = new QueryBuilder();
            foreach (var kvp in context.Request.Query)
            {
                if (string.Equals(kvp.Key, "ssr", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var value in kvp.Value)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        qb.Add(kvp.Key, value);
                    }
                }
            }

            context.Response.Redirect(BedrijvenkaartSsrPreview.PreviewPath + qb.ToQueryString());
            return Task.CompletedTask;
        }

        return next(context);
    }
}

public static class BedrijvenkaartSsrPreviewMiddlewareExtensions
{
    public static IApplicationBuilder UseBedrijvenkaartSsrPreviewRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<BedrijvenkaartSsrPreviewMiddleware>();
}
