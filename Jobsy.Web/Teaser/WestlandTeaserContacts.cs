namespace Jobsy.Web.Teaser;

/// <summary>
/// Contact settings for the Westland teaser landing. Override WhatsApp via
/// <c>TeaserLanding:WhatsAppE164</c> in appsettings (digits only, country code, no +).
/// </summary>
public static class WestlandTeaserContacts
{
    /// <summary>Fallback when config is empty — replace before go-live.</summary>
    public const string DefaultWhatsAppE164 = "31600000000";

    public const string DefaultWhatsAppMessage =
        "Hoi Dennis! Ik heb een vraag over Lobsy in het Westland.";

    public static string BuildWhatsAppUrl(string? e164FromConfig)
    {
        var digits = new string((e164FromConfig ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 10)
        {
            digits = DefaultWhatsAppE164;
        }

        return $"https://wa.me/{digits}?text={Uri.EscapeDataString(DefaultWhatsAppMessage)}";
    }
}
