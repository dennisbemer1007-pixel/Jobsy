using System.Globalization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Hosting;

/// <summary>
/// While the admin switch is on (errors 05 §05.3), visitors get a real 503 with the friendly
/// onderhoudspagina in their own language; admins pass through and see a banner instead.
/// <para>
/// HTML requests are answered by setting 503 and letting <see cref="ErrorPagesExtensions"/>
/// re-execute <c>/status/503</c>, so the page comes from the same renderer as 404 and 500 and
/// still makes no API or database call. Everything else gets an empty 503.
/// </para>
/// </summary>
public sealed class MaintenanceMiddleware
{
    /// <summary>Prefixes that keep working while maintenance is on.</summary>
    private static readonly string[] AllowedPrefixes =
    [
        "/login",
        "/account",
        "/status",
        "/Error",
        "/api/auth",
        "/_framework",
        "/_content",
        "/css",
        "/js",
        "/img",
        "/lib",
        "/fonts"
    ];

    private static readonly string[] AllowedExactPaths =
    [
        "/healthz",
        "/robots.txt",
        "/favicon.ico",
        "/.well-known/security.txt"
    ];

    private readonly RequestDelegate _next;

    public MaintenanceMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, MaintenanceState state)
    {
        if (!state.IsEnabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";
        var isAdmin = IsAdmin(context);
        if (isAdmin || IsAllowed(path, isAdmin))
        {
            await _next(context);
            return;
        }

        var retryAfter = state.RetryAfterSeconds;
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
        ErrorResponse.ApplyHeaders(context);

        // An HTML request is picked up by UseHtmlStatusCodePages on the way out and re-executed
        // through /status/503; anything else is left as a bare 503.
    }

    /// <summary>Static assets, the status pages and the admin sign-in path stay reachable.</summary>
    public static bool IsAllowed(string path, bool isAdmin)
    {
        foreach (var exact in AllowedExactPaths)
        {
            if (string.Equals(path, exact, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var prefix in AllowedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (path.Length == prefix.Length || path[prefix.Length] is '/'))
            {
                return true;
            }
        }

        // The circuit is only useful to someone who may still use the app.
        if (path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase))
        {
            return isAdmin;
        }

        return false;
    }

    /// <summary>Role check on the auth cookie principal only — no API or database call.</summary>
    public static bool IsAdmin(HttpContext context)
        => context.User.Identity?.IsAuthenticated == true
           && context.User.IsInRole(JobsyRoles.Admin);

    /// <summary>Seconds for the page's capped <c>meta http-equiv="refresh"</c> (05.4).</summary>
    public static int MetaRefreshSeconds(int retryAfterSeconds)
        => Math.Min(retryAfterSeconds, MaintenanceRules.MaxMetaRefreshSeconds);
}
