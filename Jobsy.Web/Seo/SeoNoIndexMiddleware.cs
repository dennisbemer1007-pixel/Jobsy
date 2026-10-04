namespace Jobsy.Web.Seo;

/// <summary>
/// When <c>Seo:NoIndex</c> is true, every response tells crawlers to stay away.
/// Acceptatie sets this. Production leaves it off. Do not key it on <c>IsProduction()</c>:
/// both environments run with <c>ASPNETCORE_ENVIRONMENT=Production</c>.
/// </summary>
public sealed class SeoNoIndexMiddleware
{
    public const string HeaderName = "X-Robots-Tag";
    public const string HeaderValue = "noindex, nofollow";

    private readonly RequestDelegate _next;

    public SeoNoIndexMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Seo:NoIndex"))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = HeaderValue;
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
