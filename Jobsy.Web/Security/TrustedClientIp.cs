using System.Net;

namespace Jobsy.Web.Security;

/// <summary>
/// Trusted visitor IP after forwarded-headers + Cloudflare origin enforcement.
/// Never read <c>CF-Connecting-IP</c> or <c>X-Forwarded-For</c> directly elsewhere.
/// </summary>
public static class TrustedClientIp
{
    public static string? Resolve(HttpContext? http)
    {
        if (http is null)
        {
            return null;
        }

        return http.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>Normalize IPv6 to a /64 prefix for rate-limit partition keys only.</summary>
    public static string PartitionKey(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip.Trim(), out var address))
        {
            return "unknown";
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            if (bytes.Length == 16)
            {
                for (var i = 8; i < 16; i++)
                {
                    bytes[i] = 0;
                }

                return new IPAddress(bytes).ToString();
            }
        }

        return address.ToString();
    }
}
