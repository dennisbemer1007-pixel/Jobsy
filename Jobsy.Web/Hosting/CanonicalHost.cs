using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Hosting;

public static class CanonicalHost
{
    public const string DefaultApex = "lobsy.nl";

    public static bool TryStripWww(string host, out string canonical)
    {
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && host.Length > 4)
        {
            canonical = host[4..];
            return true;
        }

        canonical = host;
        return false;
    }

    public static bool IsLoopback(string host)
        => host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
           || host.StartsWith("127.", StringComparison.Ordinal)
           || host.Equals("::1", StringComparison.Ordinal);

    /// <summary>
    /// Only rewrite www → apex for hosts we actually serve. Production uses
    /// AllowedHosts=* (Render health checks), so an unfiltered Host-based 301
    /// would be an open redirect.
    /// </summary>
    public static bool ShouldRedirectWww(string host, IEnumerable<string>? extraApexHosts = null)
    {
        if (IsLoopback(host) || !TryStripWww(host, out var canonical) || IsLoopback(canonical))
        {
            return false;
        }

        if (IsAllowedApex(canonical, DefaultApex))
        {
            return true;
        }

        if (extraApexHosts is null)
        {
            return false;
        }

        foreach (var apex in extraApexHosts)
        {
            if (!string.IsNullOrWhiteSpace(apex) && IsAllowedApex(canonical, apex.Trim()))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<string> ConfiguredApexHosts(IConfiguration config)
    {
        var hosts = new List<string>();
        var publicWeb = config["PublicWebBaseUrl"];
        if (Uri.TryCreate(publicWeb, UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.Host)
            && !IsLoopback(uri.Host))
        {
            var apex = TryStripWww(uri.Host, out var stripped) ? stripped : uri.Host;
            if (!IsAllowedApex(apex, DefaultApex))
            {
                hosts.Add(apex);
            }
        }

        return hosts;
    }

    private static bool IsAllowedApex(string host, string apex)
    {
        return host.Equals(apex, StringComparison.OrdinalIgnoreCase)
               || host.EndsWith("." + apex, StringComparison.OrdinalIgnoreCase);
    }
}
