using Jobsy.Web.Navigation;

namespace Jobsy.Web.Auth;

/// <summary>
/// Legacy <c>/banen</c> → permanent redirect to <see cref="PublicRoutes.Banenkaart"/>,
/// preserving the query string.
/// </summary>
public sealed class BanenRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.Value;
            if (string.Equals(path, "/banen", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/banen/", StringComparison.OrdinalIgnoreCase))
            {
                var location = PublicRoutes.Banenkaart;
                if (context.Request.QueryString.HasValue)
                {
                    location += context.Request.QueryString.Value;
                }

                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = location;
                return;
            }
        }

        await next(context);
    }
}

public static class BanenRedirectExtensions
{
    public static IApplicationBuilder UseBanenRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<BanenRedirectMiddleware>();
}
