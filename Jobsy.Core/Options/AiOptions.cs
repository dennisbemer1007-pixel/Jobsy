namespace Jobsy.Core.Options;

/// <summary>
/// Which company receives AI calls. Set with <c>Ai__Provider</c> (<c>OpenAI</c> or <c>Mistral</c>).
/// The default is OpenAI, so nothing changes until the env var is switched.
/// An unknown value is treated as OpenAI and logged once by the endpoint resolver.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = nameof(AiProvider.OpenAI);

    public AiProvider Resolved => AiProviderParser.Parse(Provider);

    public bool IsKnownProvider => AiProviderParser.IsKnown(Provider);
}

public enum AiProvider
{
    OpenAI = 0,
    Mistral = 1
}

public static class AiProviderParser
{
    public static bool IsKnown(string? raw)
    {
        var value = raw?.Trim();
        return string.Equals(value, nameof(AiProvider.OpenAI), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(AiProvider.Mistral), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Unknown or empty values stay on OpenAI.</summary>
    public static AiProvider Parse(string? raw)
        => string.Equals(raw?.Trim(), nameof(AiProvider.Mistral), StringComparison.OrdinalIgnoreCase)
            ? AiProvider.Mistral
            : AiProvider.OpenAI;
}

/// <summary>Read-only label for the admin integrations screen. The switch itself is env-only.</summary>
public static class AiAdminStatus
{
    public static (string Provider, string Model) Describe(
        AiOptions? ai,
        MistralOptions? mistral,
        string? openAiModel)
    {
        if (ai?.Resolved == AiProvider.Mistral)
        {
            var model = string.IsNullOrWhiteSpace(mistral?.Model)
                ? MistralOptions.DefaultModel
                : mistral.Model.Trim();
            return ("Mistral", model);
        }

        var openAi = string.IsNullOrWhiteSpace(openAiModel)
            ? OpenAiOptions.DefaultModel
            : openAiModel.Trim();
        return ("OpenAI", openAi);
    }
}
