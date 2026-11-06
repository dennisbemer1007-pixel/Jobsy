namespace Jobsy.Core.Options;

/// <summary>
/// WhatsApp Business Cloud API settings. Values come from configuration or environment
/// variables. Never hard-code a token or phone-number id.
/// </summary>
public sealed class WhatsAppReminderOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>Env <c>WHATSAPP_PHONE_NUMBER_ID</c> or <c>WhatsApp:PhoneNumberId</c>.</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Env <c>WHATSAPP_ACCESS_TOKEN</c> or <c>WhatsApp:AccessToken</c>.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Graph version path segment, for example <c>v21.0</c>.</summary>
    public string GraphVersion { get; set; } = "v21.0";

    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
