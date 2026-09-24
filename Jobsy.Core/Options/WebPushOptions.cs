namespace Jobsy.Core.Options;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    /// <summary>Contact URI for VAPID (mailto: or https:).</summary>
    public string Subject { get; set; } = "mailto:support@lobsy.nl";

    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
}
