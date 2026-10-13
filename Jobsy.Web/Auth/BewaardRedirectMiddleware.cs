namespace Jobsy.Web.Auth;

/// <summary>
/// Legacy / guessed Bewaard URLs → 302 <c>/candidate/liked</c> (query string kept).
/// </summary>
public sealed class BewaardRedirectMiddleware(RequestDelegate next)
{
    private static readonly PathString[] LegacyPaths =
    [
        new("/bewaard"),
        new("/candidate/saved"),
        new("/candidate/bewaard")
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path;
            foreach (var legacy in LegacyPaths)
            {
                if (!path.Equals(legacy, StringComparison.OrdinalIgnoreCase)
                    && !path.Equals(legacy.Add("/"), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var location = "/candidate/liked";
                if (context.Request.QueryString.HasValue)
                {
                    location += context.Request.QueryString.Value;
                }

                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = location;
                return;
            }
        }

        await next(context);
    }
}

public static class BewaardRedirectExtensions
{
    public static IApplicationBuilder UseBewaardRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<BewaardRedirectMiddleware>();
}
