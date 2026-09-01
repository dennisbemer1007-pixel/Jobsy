namespace Jobsy.Web.Security;

/// <summary>
/// Builds the public-web CSP. Scripts use a per-request nonce (no
/// <c>'unsafe-inline'</c>). Style attributes stay <c>'unsafe-inline'</c> because
/// Razor/MapLibre set CSS variables inline; <c>&lt;style&gt;</c> elements use the nonce.
/// Image and connect hosts are allow-listed (no <c>https:</c> / <c>ws:</c> / <c>wss:</c>
/// scheme wildcards). Blazor Server still needs <c>'unsafe-eval'</c> for the circuit.
/// </summary>
public static class JobsyContentSecurityPolicy
{
    public const string OpenFreeMap = "https://tiles.openfreemap.org";
    public const string Picsum = "https://picsum.photos";
    public const string PicsumFastly = "https://fastly.picsum.photos";
    public const string PicsumI = "https://i.picsum.photos";

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
            $"img-src 'self' data: blob: {Picsum} {PicsumFastly} {PicsumI} {OpenFreeMap};",
            $"font-src 'self' data: {OpenFreeMap};",
            $"style-src-elem 'self' {n};",
            "style-src-attr 'unsafe-inline';",
            $"script-src 'self' {n} 'unsafe-eval';",
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
