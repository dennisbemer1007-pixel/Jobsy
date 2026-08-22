namespace Jobsy.Web.Security;

/// <summary>
/// Browser security headers for the Blazor host. CSP is nonce-based for scripts
/// so the document never needs <c>script-src 'unsafe-inline'</c>.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext context)
    {
        var nonce = CspNonce.GetOrCreate(context);
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
        context.Response.Headers.ContentSecurityPolicy = JobsyContentSecurityPolicy.ForWeb(nonce);
        return next(context);
    }
}
