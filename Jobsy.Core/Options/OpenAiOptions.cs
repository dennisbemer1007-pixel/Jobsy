namespace Jobsy.Core.Options;

/// <summary>
/// Optional OpenAI settings for vacancy content moderation, mock interviews, CV extraction,
/// and general-occupation career-compass generation after the paid 150-item beroepentest.
/// Without an API key those features use local fallbacks.
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public const string DefaultModel = "gpt-4o-mini";

    public const string DefaultBaseUrl = "https://api.openai.com/v1/";

    /// <summary>
    /// Bearer token for api.openai.com. Leave empty to use local fallbacks only.
    /// Ignored when <see cref="AiOptions.Provider"/> is Mistral; that path uses <c>Mistral__ApiKey</c>.
    /// </summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = DefaultModel;

    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
