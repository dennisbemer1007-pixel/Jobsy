using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;

namespace Jobsy.Core.Email;

/// <summary>
/// Mail footer fields. <c>Mail:LegalAddress</c> and <c>Mail:KvkNumber</c> win when set.
/// Empty fields are filled from company details (Bedrijfsgegevens), which already
/// fall back to <c>Legal:*</c>.
/// </summary>
public static class MailLegalFooter
{
    public static void ApplyIdentity(MailOptions options, LegalIdentitySnapshot identity)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(identity);

        if (string.IsNullOrWhiteSpace(options.LegalName)
            || string.Equals(options.LegalName.Trim(), MailOptions.DefaultLegalName, StringComparison.Ordinal))
        {
            options.LegalName = identity.DisplayName;
        }

        if (string.IsNullOrWhiteSpace(options.LegalAddress) && identity.AddressLine is not null)
        {
            options.LegalAddress = identity.AddressLine;
        }

        if (string.IsNullOrWhiteSpace(options.KvkNumber) && identity.KvkNumber is not null)
        {
            options.KvkNumber = identity.KvkNumber;
        }

        if (string.IsNullOrWhiteSpace(options.SupportAddress))
        {
            options.SupportAddress = identity.SupportEmail;
        }
    }

    /// <summary>
    /// True when the footer the API would send still lacks an address or a KvK number.
    /// Does not change <paramref name="mail"/> (those options are cached for the process).
    /// </summary>
    public static bool IsMissing(MailOptions mail, LegalIdentitySnapshot? identity = null)
    {
        ArgumentNullException.ThrowIfNull(mail);
        if (identity is null)
        {
            return mail.MissingLegalFooter;
        }

        var probe = new MailOptions
        {
            LegalName = mail.LegalName,
            LegalAddress = mail.LegalAddress,
            KvkNumber = mail.KvkNumber,
            SupportAddress = mail.SupportAddress
        };
        ApplyIdentity(probe, identity);
        return probe.MissingLegalFooter;
    }
}
