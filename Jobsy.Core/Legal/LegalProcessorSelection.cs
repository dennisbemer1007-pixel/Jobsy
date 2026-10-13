using Jobsy.Core.Ai;
using Jobsy.Core.Email;

namespace Jobsy.Core.Legal;

/// <summary>
/// The privacy page lists the processors that actually run.
/// Resend and Lettermint share one mail slot. OpenAI and Mistral share one AI slot.
/// Mistral inference stays in the EU only when <see cref="MistralEndpoint.InferenceStaysInEu"/> is true.
/// </summary>
public static class LegalProcessorSelection
{
    public static IReadOnlyList<LegalProcessor> Resolve(string? mailProvider, bool lettermintApiKeyConfigured)
        => Resolve(mailProvider, lettermintApiKeyConfigured, AiProviderKind.OpenAI, mistralBaseUrl: null);

    public static IReadOnlyList<LegalProcessor> Resolve(
        string? mailProvider,
        bool lettermintApiKeyConfigured,
        AiProviderKind aiProvider,
        string? mistralBaseUrl)
    {
        var mailChoice = MailProviderChoice.Choose(mailProvider, lettermintApiKeyConfigured);
        var activeMail = mailChoice.Kind switch
        {
            MailProviderKind.Lettermint => MailProviderNames.Lettermint,
            MailProviderKind.Resend => MailProviderNames.Resend,
            _ => null
        };
        var activeAi = aiProvider switch
        {
            AiProviderKind.Mistral => AiProviderNames.Mistral,
            AiProviderKind.OpenAI => AiProviderNames.OpenAI,
            _ => null
        };
        var mistralInEu = aiProvider == AiProviderKind.Mistral
            && MistralEndpoint.InferenceStaysInEu(mistralBaseUrl);

        return LegalProcessors.All
            .Where(processor => processor.WhenMailProvider is null
                || (activeMail is not null
                    && string.Equals(processor.WhenMailProvider, activeMail, StringComparison.OrdinalIgnoreCase)))
            .Where(processor => processor.WhenAiProvider is null
                || (activeAi is not null
                    && string.Equals(processor.WhenAiProvider, activeAi, StringComparison.OrdinalIgnoreCase)))
            .Select(processor => ApplyMistralHost(processor, mistralInEu))
            .ToList();
    }

    /// <summary>
    /// The catalog row stores the non-EU Mistral endpoint. The EU host rewrites that one row.
    /// </summary>
    public static LegalProcessor ApplyMistralHost(LegalProcessor processor, bool mistralDataStaysInTheEu)
    {
        if (!mistralDataStaysInTheEu
            || !string.Equals(processor.Id, "mistral", StringComparison.Ordinal))
        {
            return processor;
        }

        return processor with
        {
            DataRegion = ProcessorRegion.EuropeanUnion,
            TransferBasisKey = LegalProcessors.InsideEu,
            DataKey = "Legal.Processor.mistral.Data",
            LocationNoteKey = "Legal.Processor.mistral.EuNote"
        };
    }
}
