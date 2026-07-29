using Jobsy.Core.Entities;

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
        "Vul een telefoonnummer in wanneer Telefoon is aangevinkt.";

    public const string WhatsAppRequiresNumber =
        "Vul een WhatsApp-nummer in (of telefoonnummer) wanneer WhatsApp is aangevinkt.";

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

        if (preferMail && string.IsNullOrWhiteSpace(contactEmail))
        {
            return MailRequiresEmail;
        }

        if (preferPhone && string.IsNullOrWhiteSpace(contactPhone))
        {
            return PhoneRequiresNumber;
        }

        if (preferWhatsApp
            && string.IsNullOrWhiteSpace(contactWhatsApp)
            && string.IsNullOrWhiteSpace(contactPhone))
        {
            return WhatsAppRequiresNumber;
        }

        return null;
    }

    /// <summary>
    /// Effective contact options for a vacancy after a successful application.
    /// Returns unavailable when the employer did not opt in or no usable channel remains.
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
        else if (company.DirectContactEnabled || parent is null || !parent.DirectContactEnabled)
        {
            enabled = company.DirectContactEnabled;
            mail = company.ContactPreferMail;
            phone = company.ContactPreferPhone;
            whatsApp = company.ContactPreferWhatsApp;
        }
        else
        {
            enabled = parent.DirectContactEnabled;
            mail = parent.ContactPreferMail;
            phone = parent.ContactPreferPhone;
            whatsApp = parent.ContactPreferWhatsApp;
        }

        var email = FirstNonEmpty(company.ContactEmail, parent?.ContactEmail);
        var phoneNumber = FirstNonEmpty(company.ContactPhone, parent?.ContactPhone);
        var whatsAppNumber = FirstNonEmpty(company.ContactWhatsApp, parent?.ContactWhatsApp, phoneNumber);

        if (string.IsNullOrWhiteSpace(email))
        {
            mail = false;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            phone = false;
        }

        if (string.IsNullOrWhiteSpace(whatsAppNumber))
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
            Email: mail ? email : null,
            Phone: phone ? phoneNumber : null,
            WhatsAppNumber: whatsApp ? DigitsOnly(whatsAppNumber) : null);
    }

    public static string? DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? null : digits;
    }

    public static string? WhatsAppUrl(string? digitsOrPhone)
    {
        var digits = DigitsOnly(digitsOrPhone);
        return digits is null ? null : $"https://wa.me/{digits}";
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
