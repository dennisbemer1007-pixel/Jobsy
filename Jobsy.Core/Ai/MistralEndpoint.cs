using System.Net;
using System.Net.Sockets;
using Jobsy.Core.Options;

namespace Jobsy.Core.Ai;

/// <summary>
/// Where a Mistral call actually goes. EU inference is the host <c>api.eu.mistral.ai</c>,
/// not a console setting. The global host <c>api.mistral.ai</c> does not promise a place.
/// </summary>
public static class MistralEndpoint
{
    public const string EuHost = "api.eu.mistral.ai";

    /// <summary>
    /// The base URL the resolver will call. An empty, invalid, or private URL falls back to
    /// <see cref="MistralOptions.DefaultBaseUrl"/>, which is the EU regional endpoint.
    /// </summary>
    public static string EffectiveBaseUrl(string? configured)
    {
        var raw = string.IsNullOrWhiteSpace(configured)
            ? MistralOptions.DefaultBaseUrl
            : configured.Trim();
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || IsBlockedHost(uri.IdnHost))
        {
            return MistralOptions.DefaultBaseUrl;
        }

        return uri.AbsoluteUri.TrimEnd('/') + "/";
    }

    /// <summary>True only when the call is sent to <see cref="EuHost"/>.</summary>
    public static bool InferenceStaysInEu(string? configured)
    {
        var effective = EffectiveBaseUrl(configured);
        return Uri.TryCreate(effective, UriKind.Absolute, out var uri)
            && string.Equals(uri.IdnHost, EuHost, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlockedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)
            || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var ip))
        {
            return false;
        }

        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return bytes[0] == 127
                || bytes[0] == 10
                || bytes[0] == 0
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254)
                || (bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
        }

        return ip.AddressFamily == AddressFamily.InterNetworkV6
            && (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal);
    }
}
