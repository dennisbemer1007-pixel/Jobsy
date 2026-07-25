namespace Jobsy.Core.Options;

/// <summary>
/// Optional OpenAI settings for vacancy content moderation and mock interviews.
/// Without an API key moderation uses local heuristics; mock interviews use a scripted fallback.
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>Bearer token for api.openai.com. Leave empty to use local fallbacks only.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
