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

    /// <summary>
    /// Quality model (stories, coach, plans, fit, practice interview).
    /// Env: <c>OpenAI__Model</c>. The OpenAI tile in Admin wins over this value.
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Optional cheaper model for translation, CV extraction and vacancy moderation.
    /// Empty means those features use the same model as quality calls.
    /// Env: <c>OpenAI__SmallModel</c>. <c>Ai__SmallModel</c> is used when this is empty.
    /// A value on the OpenAI tile wins over both.
    /// </summary>
    public string? SmallModel { get; set; }

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
