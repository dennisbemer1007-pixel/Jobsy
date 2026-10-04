using Jobsy.Core.Ai;

namespace Jobsy.Core.Legal;

/// <summary>
/// The privacy page lists the AI company that actually receives calls.
/// OpenAI and Mistral share one slot. The id <c>mistral</c> is the token a host watch
/// uses for <c>api.mistral.ai</c> and <c>api.eu.mistral.ai</c>.
/// The Mistral row's place follows the base URL host: only <c>api.eu.mistral.ai</c>
/// says the inference runs in the EU.
/// Mail rows are not filtered here; <see cref="LegalProcessorSelection"/> applies both slots.
/// </summary>
public static class LegalAiProcessorSelection
{
    public static IReadOnlyList<LegalProcessor> Resolve(AiProviderKind effective, string? mistralBaseUrl = null)
    {
        var name = effective == AiProviderKind.Mistral
            ? AiProviderNames.Mistral
            : AiProviderNames.OpenAI;
        var mistralInEu = effective == AiProviderKind.Mistral
            && MistralEndpoint.InferenceStaysInEu(mistralBaseUrl);

        return LegalProcessors.All
            .Where(processor => processor.WhenAiProvider is null
                || string.Equals(processor.WhenAiProvider, name, StringComparison.OrdinalIgnoreCase))
            .Select(processor => LegalProcessorSelection.ApplyMistralHost(processor, mistralInEu))
            .ToList();
    }

    public static IReadOnlyList<LegalProcessor> Resolve(
        string? provider,
        string? mistralApiKey,
        string? mistralBaseUrl = null)
        => Resolve(AiProviderChoice.Decide(provider, mistralApiKey).Kind, mistralBaseUrl);
}
