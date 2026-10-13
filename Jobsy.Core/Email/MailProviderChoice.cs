namespace Jobsy.Core.Email;

public static class MailProviderNames
{
    public const string Resend = "Resend";
    public const string Lettermint = "Lettermint";
}

public enum MailProviderKind
{
    Resend = 0,
    Lettermint = 1,

    /// <summary>Lettermint was selected but the key is missing. Mail is not sent via Resend.</summary>
    NotConfigured = 2
}

/// <summary>
/// Lettermint is used only when it is selected and an API key is configured.
/// A missing key does not fall back to Resend. <see cref="WarnMissingLettermintKey"/>
/// tells the caller to log that mail is off.
/// </summary>
public readonly record struct MailProviderChoice(MailProviderKind Kind, bool WarnMissingLettermintKey)
{
    public bool Available => Kind is MailProviderKind.Resend or MailProviderKind.Lettermint;

    public static MailProviderChoice Choose(string? provider, bool lettermintApiKeyConfigured)
    {
        var wantsLettermint = string.Equals(
            provider?.Trim(),
            MailProviderNames.Lettermint,
            StringComparison.OrdinalIgnoreCase);
        if (!wantsLettermint)
        {
            return new MailProviderChoice(MailProviderKind.Resend, false);
        }

        if (!lettermintApiKeyConfigured)
        {
            return new MailProviderChoice(MailProviderKind.NotConfigured, true);
        }

        return new MailProviderChoice(MailProviderKind.Lettermint, false);
    }
}
