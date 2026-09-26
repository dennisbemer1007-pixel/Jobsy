using System.Text;

namespace Jobsy.Core.Security;

/// <summary>Derives a short calm device label from a User-Agent string.</summary>
public static partial class DeviceNameFormatter
{
    public static string FromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Onbekend apparaat";
        }

        var ua = userAgent.Trim();
        var device = ResolveDevice(ua);
        var browser = ResolveBrowser(ua);
        if (string.IsNullOrEmpty(browser))
        {
            return device;
        }

        return $"{device} – {browser}";
    }

    private static string ResolveDevice(string ua)
    {
        if (Contains(ua, "iPhone")) return "iPhone";
        if (Contains(ua, "iPad")) return "iPad";
        if (Contains(ua, "Android") && Contains(ua, "Mobile")) return "Android-telefoon";
        if (Contains(ua, "Android")) return "Android-tablet";
        if (Contains(ua, "Macintosh") || Contains(ua, "Mac OS")) return "Mac";
        if (Contains(ua, "Windows")) return "Windows-pc";
        if (Contains(ua, "CrOS")) return "Chromebook";
        if (Contains(ua, "Linux")) return "Linux";
        return "Apparaat";
    }

    private static string ResolveBrowser(string ua)
    {
        // Order matters: Edge/Opera/Samsung include Chrome; Chrome includes Safari token.
        if (Contains(ua, "Edg/") || Contains(ua, "EdgA/") || Contains(ua, "EdgiOS/")) return "Edge";
        if (Contains(ua, "OPR/") || Contains(ua, "Opera")) return "Opera";
        if (Contains(ua, "SamsungBrowser")) return "Samsung Internet";
        if (Contains(ua, "Firefox/") || Contains(ua, "FxiOS/")) return "Firefox";
        if (Contains(ua, "CriOS/") || (Contains(ua, "Chrome/") && !Contains(ua, "Chromium"))) return "Chrome";
        if (Contains(ua, "Safari/") && !Contains(ua, "Chrome") && !Contains(ua, "CriOS")) return "Safari";
        return "";
    }

    private static bool Contains(string haystack, string needle)
        => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
