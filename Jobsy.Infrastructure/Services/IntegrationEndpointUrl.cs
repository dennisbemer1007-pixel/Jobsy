using System.Net;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Validates admin-configured integration base URLs to reduce SSRF risk.
/// </summary>
public static class IntegrationEndpointUrl
{
    public static bool TryNormalizeBaseUrl(string? value, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            error = "Base URL moet een absolute http(s)-URL zijn.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "Base URL mag geen credentials bevatten.";
            return false;
        }

        if (IsBlockedHost(uri.Host))
        {
            error = "Base URL mag niet naar een privé- of lokale host wijzen.";
            return false;
        }

        normalized = uri.AbsoluteUri.TrimEnd('/') + "/";
        return true;
    }

    public static bool IsBlockedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return true;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(host, out var ip))
        {
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            // IPv4 loopback 127.0.0.0/8 (IsLoopback only covers 127.0.0.1 on some runtimes).
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes[0] == 127)
                {
                    return true;
                }

                if (bytes[0] == 10
                    || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 169 && bytes[1] == 254)
                    || (bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
                    || (bytes[0] == 0))
                {
                    return true;
                }
            }

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
                {
                    return true;
                }
            }

            return false;
        }

        // Unresolved hostnames that look like private DNS suffixes already handled;
        // remaining hostnames are allowed (public APIs).
        return false;
    }
}
