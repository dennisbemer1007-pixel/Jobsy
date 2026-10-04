namespace Jobsy.Core.Ai;

public enum AiProviderKind
{
    OpenAI,
    Mistral
}

/// <summary>Config values for <c>Ai:Provider</c>. Compared without case.</summary>
public static class AiProviderNames
{
    public const string OpenAI = "OpenAI";
    public const string Mistral = "Mistral";
}

/// <summary>
/// Which AI company actually receives calls.
/// Mistral is used only when <c>Ai:Provider</c> is Mistral and <c>Mistral:ApiKey</c> is set.
/// A missing key stays on OpenAI, so the privacy list does not name a company we do not call.
/// </summary>
public static class AiProviderChoice
{
    public static AiProviderDecision Decide(string? provider, string? mistralApiKey)
        => AiProviderDecision.Decide(provider, mistralApiKey);
}

public readonly record struct AiProviderDecision(
    AiProviderKind Kind,
    bool RequestedMistralWithoutKey,
    bool UnknownProvider)
{
    public string Name
        => Kind == AiProviderKind.Mistral ? AiProviderNames.Mistral : AiProviderNames.OpenAI;

    public static AiProviderDecision Decide(string? provider, string? mistralApiKey)
    {
        var raw = provider?.Trim();
        if (string.IsNullOrEmpty(raw)
            || raw.Equals(AiProviderNames.OpenAI, StringComparison.OrdinalIgnoreCase))
        {
            return new AiProviderDecision(AiProviderKind.OpenAI, false, false);
        }

        if (raw.Equals(AiProviderNames.Mistral, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(mistralApiKey))
            {
                return new AiProviderDecision(AiProviderKind.OpenAI, true, false);
            }

            return new AiProviderDecision(AiProviderKind.Mistral, false, false);
        }

        return new AiProviderDecision(AiProviderKind.OpenAI, false, true);
    }
}
