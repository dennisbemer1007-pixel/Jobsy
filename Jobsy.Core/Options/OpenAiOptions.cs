namespace Jobsy.Core.Options;

/// <summary>
/// Optional OpenAI settings for vacancy content moderation, mock interviews, CV extraction,
/// and general-occupation career-compass generation after the paid 150-item beroepentest.
/// Without an API key those features use local fallbacks.
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>Bearer token for api.openai.com. Leave empty to use local fallbacks only.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
