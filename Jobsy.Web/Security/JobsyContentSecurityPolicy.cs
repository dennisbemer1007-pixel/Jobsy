namespace Jobsy.Web.Security;

/// <summary>
/// Builds the public-web CSP. Scripts use a per-request nonce (no
/// <c>'unsafe-inline'</c>). Style attributes stay <c>'unsafe-inline'</c> because
/// Razor/MapLibre set CSS variables inline; <c>&lt;style&gt;</c> elements use the nonce.
/// </summary>
public static class JobsyContentSecurityPolicy
{
    public static string ForWeb(string nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        var n = $"'nonce-{nonce}'";
        return string.Join(' ',
            "default-src 'self';",
            "base-uri 'self';",
            "frame-ancestors 'none';",
            "frame-src 'self' https://www.youtube-nocookie.com https://www.youtube.com https://player.vimeo.com https://vimeo.com;",
            "object-src 'none';",
            "img-src 'self' data: https: blob:;",
            "font-src 'self' data: https://fonts.gstatic.com https://tiles.openfreemap.org;",
            $"style-src-elem 'self' {n} https://fonts.googleapis.com;",
            "style-src-attr 'unsafe-inline';",
            $"script-src 'self' {n} 'unsafe-eval';",
            "script-src-attr 'none';",
            "connect-src 'self' wss: ws: https:;",
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
