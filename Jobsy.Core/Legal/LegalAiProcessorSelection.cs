using Jobsy.Core.Ai;

namespace Jobsy.Core.Legal;

/// <summary>
/// The privacy page lists the AI company that actually receives calls.
/// OpenAI and Mistral share one slot. The id <c>mistral</c> stays, for the host watch on
/// <c>mistral.ai</c>. The Mistral row's place follows the base URL host: only
/// <c>api.eu.mistral.ai</c> says the inference runs in the EU.
/// </summary>
public static class LegalAiProcessorSelection
{
    public const string GlobalRegion =
        "Frankrijk (Parijs); Mistral belooft geen plek voor de verwerking. Account en facturen van Mistral kunnen buiten de EU staan.";

    public static IReadOnlyList<LegalProcessor> Resolve(AiProviderKind effective, string? mistralBaseUrl = null)
    {
        var name = effective == AiProviderKind.Mistral
            ? AiProviderNames.Mistral
            : AiProviderNames.OpenAI;
        var eu = MistralEndpoint.InferenceStaysInEu(mistralBaseUrl);

        return LegalProcessors.All
            .Where(processor => processor.WhenAiProvider is null
                || string.Equals(processor.WhenAiProvider, name, StringComparison.OrdinalIgnoreCase))
            .Select(processor => ApplyResidence(processor, eu))
            .ToList();
    }

    public static IReadOnlyList<LegalProcessor> Resolve(string? provider, string? mistralApiKey, string? mistralBaseUrl = null)
        => Resolve(AiProviderChoice.Decide(provider, mistralApiKey).Kind, mistralBaseUrl);

    private static LegalProcessor ApplyResidence(LegalProcessor processor, bool euInference)
    {
        if (!string.Equals(processor.Id, "mistral", StringComparison.Ordinal) || euInference)
        {
            return processor;
        }

        return processor with
        {
            Region = GlobalRegion,
            DataKey = "Legal.Processor.mistral.Data.Global",
            TransferBasisKey = LegalProcessors.NoStatedPlace
        };
    }
}
