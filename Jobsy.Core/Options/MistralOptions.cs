namespace Jobsy.Core.Options;

/// <summary>
/// Mistral AI (Paris, EU) credentials. Used only when <see cref="AiOptions.Provider"/> is <c>Mistral</c>.
/// Env: <c>Mistral__ApiKey</c> (alias <c>MISTRAL_API_KEY</c>), optional <c>Mistral__Model</c> and <c>Mistral__BaseUrl</c>.
/// The OpenAI key stored in admin integrations is not sent to Mistral.
/// </summary>
/// <remarks>
/// Chat completions, including <c>response_format: json_object</c>, use the same request shape as OpenAI.
/// Gaps, on purpose:
/// embeddings are not called (a future call would use model <c>mistral-embed</c>);
/// vacancy moderation stays a chat JSON call, not OpenAI's moderations endpoint and not Mistral's moderation model;
/// no vision payloads (a CV is text before it is sent);
/// voorlezen stays the browser <c>speechSynthesis</c> API. Mistral Voxtral TTS is not used.
/// </remarks>
public sealed class MistralOptions
{
    public const string SectionName = "Mistral";

    /// <summary>
    /// Alias that currently resolves to Mistral Small 4 (<c>mistral-small-2603</c>).
    /// Checked against the Mistral model overview (docs.mistral.ai) in October 2026.
    /// </summary>
    public const string DefaultModel = "mistral-small-latest";

    public const string DefaultBaseUrl = "https://api.mistral.ai/v1/";

    /// <summary>Bearer token for api.mistral.ai. Empty means local fallbacks; the OpenAI key is not reused.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = DefaultModel;

    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
