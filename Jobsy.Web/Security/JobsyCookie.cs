namespace Jobsy.Web.Security;

/// <summary>
/// Cookie <c>Secure</c> behind Render/Cloudflare: Kestrel sees HTTP from the proxy,
/// but the browser connection is HTTPS. Observatory fails cookies set without Secure.
/// </summary>
public static class JobsyCookie
{
    public static bool ShouldMarkSecure(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        if (http.Request.IsHttps)
        {
            return true;
        }

        if (string.Equals(
                http.Request.Headers["X-Forwarded-Proto"],
                "https",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var host = http.Request.Host.Host;
        return host.Length > 0
               && !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
               && host != "127.0.0.1"
               && host != "::1";
    }
}
