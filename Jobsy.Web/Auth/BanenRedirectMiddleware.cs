using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Auth;

/// <summary>
/// Legacy <c>/banen</c>: ON → 301 <see cref="PublicRoutes.Banenkaart"/>; OFF → the candidate coming-soon page.
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
                var employers = context.RequestServices.GetService<IEmployersSwitch>();
                var enabled = employers is null || await employers.IsEnabledAsync(context.RequestAborted);
                if (!enabled)
                {
                    context.Response.StatusCode = StatusCodes.Status302Found;
                    context.Response.Headers.Location = FeatureRoutes.CandidateEmployersComingSoonPath;
                    return;
                }

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
