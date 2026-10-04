using Jobsy.Core.Ai;

namespace Jobsy.Core.Legal;

/// <summary>
/// The privacy page lists the AI company that actually receives calls.
/// OpenAI and Mistral share one slot. The id <c>mistral</c> is the token a host watch
/// uses for <c>api.mistral.ai</c>. <see cref="LegalProcessor.WhenAiProvider"/> is its own
/// filter, separate from any mail-provider filter added on another branch.
/// </summary>
public static class LegalAiProcessorSelection
{
    public static IReadOnlyList<LegalProcessor> Resolve(AiProviderKind effective)
    {
        var name = effective == AiProviderKind.Mistral
            ? AiProviderNames.Mistral
            : AiProviderNames.OpenAI;

        return LegalProcessors.All
            .Where(processor => processor.WhenAiProvider is null
                || string.Equals(processor.WhenAiProvider, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static IReadOnlyList<LegalProcessor> Resolve(string? provider, string? mistralApiKey)
        => Resolve(AiProviderChoice.Decide(provider, mistralApiKey).Kind);
}
