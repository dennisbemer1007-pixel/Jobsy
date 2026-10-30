namespace Jobsy.Web.Pwa;

public enum PwaEntryPlatform
{
    None = 0,
    ChromePrompt = 1,
    Samsung = 2,
    Ios = 3
}

/// <summary>Visibility rules for candidate PWA install entry points (top bar, card, sheets).</summary>
public static class PwaInstallRules
{
    public static bool IsInstalledApp(
        bool displayStandalone,
        bool displayMinimalUi,
        bool displayFullscreen,
        bool navigatorStandalone,
        bool androidAppReferrer,
        bool pwaStartUrl,
        bool relatedInstalled)
        => displayStandalone
           || displayMinimalUi
           || displayFullscreen
           || navigatorStandalone
           || androidAppReferrer
           || pwaStartUrl
           || relatedInstalled;

    public static bool IsDismissed(long? dismissedUntilUnixMs, long nowUnixMs)
    {
        if (!dismissedUntilUnixMs.HasValue)
        {
            return false;
        }

        return dismissedUntilUnixMs.Value > nowUnixMs;
    }

    public static PwaEntryPlatform DetectPlatform(
        bool isIos,
        bool isSamsungBrowser,
        bool isAndroid,
        bool canDeferredPrompt)
    {
        if (isIos)
        {
            return PwaEntryPlatform.Ios;
        }

        if (canDeferredPrompt)
        {
            return PwaEntryPlatform.ChromePrompt;
        }

        if (isAndroid && (isSamsungBrowser || !canDeferredPrompt))
        {
            return PwaEntryPlatform.Samsung;
        }

        return PwaEntryPlatform.None;
    }

    public static bool ShouldShowEntry(
        bool installed,
        long? dismissedUntilUnixMs,
        PwaEntryPlatform platform,
        long nowUnixMs)
    {
        if (installed)
        {
            return false;
        }

        if (IsDismissed(dismissedUntilUnixMs, nowUnixMs))
        {
            return false;
        }

        return platform != PwaEntryPlatform.None;
    }

    public static bool ShouldShowPostApplyCard(
        bool installed,
        long? dismissedUntilUnixMs,
        PwaEntryPlatform platform,
        bool postApplyCardPending,
        long nowUnixMs)
        => postApplyCardPending
           && ShouldShowEntry(installed, dismissedUntilUnixMs, platform, nowUnixMs);

    public static bool ShouldShowInstalledShareHint(bool installed, bool isAndroid, bool hintSeen)
        => installed && isAndroid && !hintSeen;
}
