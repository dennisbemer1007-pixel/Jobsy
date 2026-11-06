namespace Jobsy.Web.Pwa;

/// <summary>Raw install context from <c>lobsyPwaInstall.getSignals</c>.</summary>
public sealed class PwaInstallSignals
{
    public bool DisplayStandalone { get; init; }
    public bool DisplayMinimalUi { get; init; }
    public bool DisplayFullscreen { get; init; }
    public bool NavigatorStandalone { get; init; }
    public bool AndroidAppReferrer { get; init; }
    public bool PwaStartUrl { get; init; }
    public bool RelatedInstalled { get; init; }
    public bool IsIos { get; init; }
    public bool IsSamsungBrowser { get; init; }
    public bool IsAndroid { get; init; }
    public bool CanDeferredPrompt { get; init; }
    public long? EntryDismissedUntilUnixMs { get; init; }
    public bool PostApplyCardPending { get; init; }
    public bool ShareHintSeen { get; init; }

    public bool Installed => PwaInstallRules.IsInstalledApp(
        DisplayStandalone,
        DisplayMinimalUi,
        DisplayFullscreen,
        NavigatorStandalone,
        AndroidAppReferrer,
        PwaStartUrl,
        RelatedInstalled);

    public PwaEntryPlatform Platform => PwaInstallRules.DetectPlatform(
        IsIos,
        IsSamsungBrowser,
        IsAndroid,
        CanDeferredPrompt);

    public bool ShouldShowEntry(long nowUnixMs)
        => PwaInstallRules.ShouldShowEntry(Installed, EntryDismissedUntilUnixMs, Platform, nowUnixMs);

    public bool ShouldShowPostApplyCard(long nowUnixMs)
        => PwaInstallRules.ShouldShowPostApplyCard(
            Installed,
            EntryDismissedUntilUnixMs,
            Platform,
            PostApplyCardPending,
            nowUnixMs);

    public bool ShouldShowShareHint()
        => PwaInstallRules.ShouldShowInstalledShareHint(Installed, IsAndroid, ShareHintSeen);
}
