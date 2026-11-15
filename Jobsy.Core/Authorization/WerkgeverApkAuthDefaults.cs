namespace Jobsy.Core.Authorization;

/// <summary>Separate Loopbaan-APK employee cookie scheme — never shares claims with employer staff auth.</summary>
public static class WerkgeverApkAuthDefaults
{
    public const string Scheme = "WerkgeverApkEmployee";
    public const string CookieName = "Lobsy.LoopbaanApk";

    /// <summary>Sliding idle timeout for the employee session cookie.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);

    /// <summary>Absolute lifetime from issued-at claim, regardless of activity.</summary>
    public static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromMinutes(120);
}