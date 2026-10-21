namespace Jobsy.Web.Auth;

/// <summary>
/// Dutch slug and legacy guesses for candidate settings → <c>/candidate/settings</c>.
/// </summary>
public sealed class InstellingenRedirectMiddleware(RequestDelegate next)
{
    private static readonly PathString[] LegacyPaths =
    [
        new("/instellingen"),
        new("/candidate/instellingen")
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

                var location = "/candidate/settings";
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

public static class InstellingenRedirectExtensions
{
    public static IApplicationBuilder UseInstellingenRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<InstellingenRedirectMiddleware>();
}
