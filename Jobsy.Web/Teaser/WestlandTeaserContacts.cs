namespace Jobsy.Web.Teaser;

/// <summary>
/// Contact settings for the Westland teaser landing. Configure WhatsApp via
/// <c>TeaserLanding:WhatsAppE164</c> (digits only, country code, no +).
/// Returns null when unset so UI can hide CTAs (no placeholder number in production).
/// </summary>
public static class WestlandTeaserContacts
{
    public const string DefaultWhatsAppMessage =
        "Hoi Dennis! Ik heb een vraag over Lobsy in het Westland.";

    /// <summary>
    /// Builds a wa.me URL when a valid E.164 digit string is configured; otherwise null.
    /// </summary>
    public static string? TryBuildWhatsAppUrl(string? e164FromConfig)
    {
        var digits = new string((e164FromConfig ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 10 || digits is "31600000000" or "31000000000")
        {
            return null;
        }

        return $"https://wa.me/{digits}?text={Uri.EscapeDataString(DefaultWhatsAppMessage)}";
    }
}
