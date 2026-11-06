namespace Jobsy.Core.Ai;

public enum AiProviderKind
{
    OpenAI,
    Mistral,

    /// <summary>The chosen provider cannot run. Calls do not go to another company.</summary>
    Unavailable
}

/// <summary>Config values for <c>Ai:Provider</c>. Compared without case.</summary>
public static class AiProviderNames
{
    public const string OpenAI = "OpenAI";
    public const string Mistral = "Mistral";
}

/// <summary>
/// Which AI company actually receives calls.
/// OpenAI is used only when <c>Ai:Provider</c> is OpenAI (or left empty, which is that default).
/// Mistral is used only when it is selected and <c>Mistral:ApiKey</c> is set.
/// A missing or blank Mistral key does not fall back to OpenAI.
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
    public bool Available => Kind is AiProviderKind.OpenAI or AiProviderKind.Mistral;

    public string Name => Kind switch
    {
        AiProviderKind.Mistral => AiProviderNames.Mistral,
        AiProviderKind.OpenAI => AiProviderNames.OpenAI,
        _ => "Unavailable"
    };

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
            if (!IsUsableKey(mistralApiKey))
            {
                return new AiProviderDecision(AiProviderKind.Unavailable, true, false);
            }

            return new AiProviderDecision(AiProviderKind.Mistral, false, false);
        }

        return new AiProviderDecision(AiProviderKind.Unavailable, false, true);
    }

    /// <summary>Blank keys are missing. A key with only spaces is invalid.</summary>
    public static bool IsUsableKey(string? apiKey) => !string.IsNullOrWhiteSpace(apiKey);
}
