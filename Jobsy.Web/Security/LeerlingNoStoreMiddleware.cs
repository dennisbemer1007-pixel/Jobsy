namespace Jobsy.Web.Security;

/// <summary>
/// Sets Cache-Control for /leerling* before the response starts.
/// Components must not write response headers: the interactive circuit renders after headers are read-only.
/// </summary>
public sealed class LeerlingNoStoreMiddleware
{
    private readonly RequestDelegate _next;

    public LeerlingNoStoreMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/leerling"))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
