using Jobsy.Core.Email;

namespace Jobsy.Core.Legal;

/// <summary>
/// The privacy page lists the processors that actually run.
/// Resend and Lettermint share one mail slot; the active <c>Mail:Provider</c> decides which row is shown.
/// Lettermint is shown only when its API key is configured, matching the sender fallback.
/// </summary>
public static class LegalProcessorSelection
{
    public static IReadOnlyList<LegalProcessor> Resolve(string? mailProvider, bool lettermintApiKeyConfigured)
    {
        var choice = MailProviderChoice.Choose(mailProvider, lettermintApiKeyConfigured);
        var activeMail = choice.Kind == MailProviderKind.Lettermint
            ? MailProviderNames.Lettermint
            : MailProviderNames.Resend;

        return LegalProcessors.All
            .Where(processor => processor.WhenMailProvider is null
                || string.Equals(processor.WhenMailProvider, activeMail, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
