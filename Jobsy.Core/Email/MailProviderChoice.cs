namespace Jobsy.Core.Email;

public static class MailProviderNames
{
    public const string Resend = "Resend";
    public const string Lettermint = "Lettermint";
}

public enum MailProviderKind
{
    Resend = 0,
    Lettermint = 1
}

/// <summary>
/// Lettermint is used only when it is selected and an API key is configured.
/// Otherwise mail stays on Resend and <see cref="WarnMissingLettermintKey"/> is true
/// so the caller can log that fallback once.
/// </summary>
public readonly record struct MailProviderChoice(MailProviderKind Kind, bool WarnMissingLettermintKey)
{
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
            return new MailProviderChoice(MailProviderKind.Resend, true);
        }

        return new MailProviderChoice(MailProviderKind.Lettermint, false);
    }
}
