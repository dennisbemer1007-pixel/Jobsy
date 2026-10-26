namespace Jobsy.Web.Navigation;

/// <summary>Dutch aliases for candidate URLs (bookmarks, old links).</summary>
public static class CandidateLegacyRoutes
{
    public const string LegacySettingsPrefix = "/candidate/instellingen";
    public const string SettingsPath = "/candidate/settings";
    public const string LegacyNotificationsPath = "/candidate/meldingen";
    public const string NotificationsPath = "/notifications";

    public static bool TryMap(string path, out string destination)
    {
        destination = "";
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.TrimEnd('/');
        if (normalized.Length == 0)
        {
            normalized = "/";
        }

        if (string.Equals(normalized, LegacyNotificationsPath, StringComparison.OrdinalIgnoreCase))
        {
            destination = NotificationsPath;
            return true;
        }

        if (string.Equals(normalized, LegacySettingsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            destination = SettingsPath;
            return true;
        }

        if (normalized.StartsWith(LegacySettingsPrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            destination = SettingsPath + normalized[LegacySettingsPrefix.Length..];
            return true;
        }

        return false;
    }
}
