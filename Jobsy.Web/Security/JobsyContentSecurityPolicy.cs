namespace Jobsy.Web.Security;

/// <summary>
/// Builds the public-web CSP. Scripts use a per-request nonce (no
/// <c>'unsafe-inline'</c>). Style attributes stay <c>'unsafe-inline'</c> because
/// Razor/MapLibre set CSS variables inline; <c>&lt;style&gt;</c> elements use the nonce.
/// Image and connect hosts are allow-listed (no <c>https:</c> / <c>ws:</c> / <c>wss:</c>
/// scheme wildcards). App code no longer calls <c>eval</c> via JS interop (prompt 07).
/// Playwright CSP smoke on public surfaces reported no <c>securitypolicyviolation</c> events
/// without <c>'unsafe-eval'</c>, so it is omitted (see <c>docs/csp-smoke-prompt07.log</c>).
/// </summary>
public static class JobsyContentSecurityPolicy
{
    public const string OpenFreeMap = "https://tiles.openfreemap.org";

    /// <param name="allowUnsafeEval">
    /// CI/Playwright only: <c>WaitForFunctionAsync</c> evaluates predicates via <c>eval</c>.
    /// Production and Acceptatie keep this false.
    /// </param>
    public static string ForWeb(string nonce, bool allowUnsafeEval = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        var n = $"'nonce-{nonce}'";
        var scriptSrc = allowUnsafeEval
            ? $"script-src 'self' {n} 'unsafe-eval';"
            : $"script-src 'self' {n};";
        return string.Join(' ',
            "default-src 'self';",
            "base-uri 'self';",
            "frame-ancestors 'none';",
            "frame-src 'self' https://www.youtube-nocookie.com https://www.youtube.com https://player.vimeo.com https://vimeo.com;",
            "object-src 'none';",
            $"img-src 'self' data: blob: {OpenFreeMap};",
            $"font-src 'self' data: {OpenFreeMap};",
            $"style-src-elem 'self' {n};",
            "style-src-attr 'unsafe-inline';",
            scriptSrc,
            "script-src-attr 'none';",
            "form-action 'self';",
            $"connect-src 'self' {OpenFreeMap};",
            "worker-src 'self' blob:;");
    }

    public static string? ScriptSrc(string policy) => Directive(policy, "script-src");

    public static string? Directive(string policy, string name)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        foreach (var part in policy.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Equals(name, StringComparison.OrdinalIgnoreCase)
                || part.StartsWith(name + " ", StringComparison.OrdinalIgnoreCase))
            {
                return part;
            }
        }

        return null;
    }
}
