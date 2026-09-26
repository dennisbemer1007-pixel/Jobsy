namespace Jobsy.Core.Rules;

public static class DeviceSessionRules
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);
    public static readonly TimeSpan RotationGraceWindow = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan LastUsedWriteThrottle = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan HandoffLifetime = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan LocalSessionRenewalSkew = TimeSpan.FromMinutes(10);

    public const string CookieName = "Lobsy.Device";
    public const string ClaimDeviceSessionId = "device_session_id";
    public const string ClaimSessionVersion = "session_version";
    public const string ClaimHasDeviceSession = "has_device_session";

    public const string RevokeReasonLogout = "logout";
    public const string RevokeReasonLogoutAll = "logout-all";
    public const string RevokeReasonPasswordChange = "password-change";
    public const string RevokeReasonEmailChange = "email-change";
    public const string RevokeReasonAdminBlock = "admin-block";
    public const string RevokeReasonAccountDeleted = "account-deleted";
    public const string RevokeReasonTokenReuse = "token-reuse";
    public const string RevokeReasonExpired = "expired";
    public const string RevokeReasonReplaced = "rotated";
}
