using Jobsy.Core.Entities;
using System.Net.Mail;

namespace Jobsy.Core.Rules;

/// <summary>
/// Validation and resolution for employer "Voorkeur voor contact" (company + vacancy override).
/// Direct contact channels are never exposed on public vacancy payloads — only after a verified application.
/// </summary>
public static class EmployerContactPreferenceRules
{
    public const string AtLeastOneChannelRequired =
        "Kies minstens één contactoptie (Mail, Telefoon of WhatsApp) wanneer direct contact is ingeschakeld.";

    public const string MailRequiresEmail =
        "Vul een contact-e-mailadres in wanneer Mail is aangevinkt.";

    public const string PhoneRequiresNumber =
        "Vul een geldig telefoonnummer in wanneer Telefoon is aangevinkt.";

    public const string WhatsAppRequiresNumber =
        "Vul een geldig WhatsApp-nummer in (of telefoonnummer) wanneer WhatsApp is aangevinkt.";

    public const string InvalidEmail =
        "Vul een geldig e-mailadres in.";

    public static string? Validate(
        bool directContactEnabled,
        bool preferMail,
        bool preferPhone,
        bool preferWhatsApp,
        string? contactEmail = null,
        string? contactPhone = null,
        string? contactWhatsApp = null,
        bool requireContactValues = true)
    {
        if (!directContactEnabled)
        {
            return null;
        }

        if (!preferMail && !preferPhone && !preferWhatsApp)
        {
            return AtLeastOneChannelRequired;
        }

        if (!requireContactValues)
        {
            return null;
        }

        if (preferMail)
        {
            if (string.IsNullOrWhiteSpace(contactEmail))
            {
                return MailRequiresEmail;
            }

            if (!IsValidEmail(contactEmail))
            {
                return InvalidEmail;
            }
        }

        if (preferPhone && NormalizePhoneDigits(contactPhone) is null)
        {
            return PhoneRequiresNumber;
        }

        if (preferWhatsApp
            && NormalizePhoneDigits(contactWhatsApp) is null
            && NormalizePhoneDigits(contactPhone) is null)
        {
            return WhatsAppRequiresNumber;
        }

        return null;
    }

    /// <summary>
    /// Effective contact options for a vacancy after a successful application.
    /// Company (or vacancy override) flags are authoritative; parent only fills missing contact values.
    /// </summary>
    public static EffectiveEmployerContact Resolve(Company company, Vacancy vacancy, Company? parent = null)
    {
        bool enabled;
        bool mail;
        bool phone;
        bool whatsApp;

        if (vacancy.OverrideContactPreference)
        {
            enabled = vacancy.DirectContactEnabled;
            mail = vacancy.ContactPreferMail;
            phone = vacancy.ContactPreferPhone;
            whatsApp = vacancy.ContactPreferWhatsApp;
        }
        else
        {
            // Company settings win — a vestiging can turn direct contact off even if the org has it on.
            enabled = company.DirectContactEnabled;
            mail = company.ContactPreferMail;
            phone = company.ContactPreferPhone;
            whatsApp = company.ContactPreferWhatsApp;
        }

        var email = FirstNonEmpty(company.ContactEmail, parent?.ContactEmail);
        var phoneNumber = FirstNonEmpty(company.ContactPhone, parent?.ContactPhone);
        var whatsAppRaw = FirstNonEmpty(company.ContactWhatsApp, parent?.ContactWhatsApp, phoneNumber);
        var phoneDigits = NormalizePhoneDigits(phoneNumber);
        var whatsAppDigits = NormalizePhoneDigits(whatsAppRaw);

        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
        {
            mail = false;
        }

        if (phoneDigits is null)
        {
            phone = false;
        }

        if (whatsAppDigits is null)
        {
            whatsApp = false;
        }

        if (!enabled || (!mail && !phone && !whatsApp))
        {
            return EffectiveEmployerContact.Unavailable;
        }

        return new EffectiveEmployerContact(
            Available: true,
            OfferMail: mail,
            OfferPhone: phone,
            OfferWhatsApp: whatsApp,
            Email: mail ? email!.Trim() : null,
            Phone: phone ? phoneNumber!.Trim() : null,
            WhatsAppNumber: whatsApp ? whatsAppDigits : null);
    }

    /// <summary>
    /// Digits for wa.me / validation. NL mobiles starting with 0 become 31…
    /// </summary>
    public static string? NormalizePhoneDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
        {
            return null;
        }

        // Dutch national format: 06xxxxxxxx → 316xxxxxxxx
        if (digits.StartsWith('0') && digits.Length is >= 9 and <= 11)
        {
            digits = "31" + digits[1..];
        }

        return digits.Length >= 8 ? digits : null;
    }

    public static string? DigitsOnly(string? value) => NormalizePhoneDigits(value);

    public static string? WhatsAppUrl(string? digitsOrPhone)
    {
        var digits = NormalizePhoneDigits(digitsOrPhone);
        return digits is null ? null : $"https://wa.me/{digits}";
    }

    /// <summary>Human-readable international number, e.g. +31 6 12345678.</summary>
    public static string FormatDisplayPhone(string? digitsOrPhone)
    {
        var digits = NormalizePhoneDigits(digitsOrPhone);
        if (digits is null)
        {
            return string.IsNullOrWhiteSpace(digitsOrPhone) ? string.Empty : digitsOrPhone.Trim();
        }

        if (digits.StartsWith("31", StringComparison.Ordinal) && digits.Length >= 11)
        {
            return $"+31 {digits[2]} {digits[3..]}";
        }

        return digits.Length >= 8 ? "+" + digits : digits;
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}

public sealed record EffectiveEmployerContact(
    bool Available,
    bool OfferMail,
    bool OfferPhone,
    bool OfferWhatsApp,
    string? Email,
    string? Phone,
    string? WhatsAppNumber)
{
    public static EffectiveEmployerContact Unavailable { get; } =
        new(false, false, false, false, null, null, null);

    public string? WhatsAppUrl => EmployerContactPreferenceRules.WhatsAppUrl(WhatsAppNumber);
}
