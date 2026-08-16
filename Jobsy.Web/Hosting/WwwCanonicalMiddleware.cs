namespace Jobsy.Web.Hosting;

/// <summary>
/// Apex canonical: www.lobsy.nl → lobsy.nl (301). Cloudflare already does this;
/// keep a server fallback so Render/onrender hosts and misconfigured DNS still converge.
/// </summary>
public sealed class WwwCanonicalMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (CanonicalHost.IsLoopback(host) || !CanonicalHost.TryStripWww(host, out var canonical))
        {
            return next(context);
        }

        var target = Uri.UriSchemeHttps
                     + Uri.SchemeDelimiter
                     + canonical
                     + context.Request.PathBase.Value
                     + context.Request.Path.Value
                     + context.Request.QueryString.Value;
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Redirect(target, permanent: true);
        return Task.CompletedTask;
    }
}
