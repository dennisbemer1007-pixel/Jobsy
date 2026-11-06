using Jobsy.Web.Navigation;

namespace Jobsy.Web.Auth;

/// <summary>
/// Legacy test CTAs used <c>/register?van=ontdek</c> (company KvK). Redirect candidates
/// to <see cref="PublicRoutes.CreateAccountFromTest"/>; every other <c>/register</c> stays.
/// </summary>
public sealed class RegisterOntdekRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && string.Equals(context.Request.Path.Value, "/register", StringComparison.OrdinalIgnoreCase))
        {
            var van = context.Request.Query["van"].ToString();
            if (string.Equals(van, "ontdek", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = PublicRoutes.CreateAccountFromTest;
                return;
            }
        }

        await next(context);
    }
}

public static class RegisterOntdekRedirectExtensions
{
    public static IApplicationBuilder UseRegisterOntdekRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<RegisterOntdekRedirectMiddleware>();
}
