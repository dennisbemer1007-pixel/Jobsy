using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Apex canonical: www.lobsy.nl → lobsy.nl (301). Cloudflare already does this;
/// keep a server fallback for the known public host only (AllowedHosts is * on Render).
/// </summary>
public sealed class WwwCanonicalMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public Task Invoke(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (!CanonicalHost.ShouldRedirectWww(host, CanonicalHost.ConfiguredApexHosts(configuration)))
        {
            return next(context);
        }

        CanonicalHost.TryStripWww(host, out var canonical);
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
