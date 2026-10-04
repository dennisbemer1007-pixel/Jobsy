namespace Jobsy.Core.Ai;

/// <summary>Ways Lobsy could talk to a model provider.</summary>
public enum AiCapability
{
    /// <summary>POST /v1/chat/completions with a text message.</summary>
    Chat,

    /// <summary>The same chat call with <c>response_format: { "type": "json_object" }</c>.</summary>
    JsonMode,

    /// <summary>POST /v1/embeddings. Lobsy has no call site.</summary>
    Embeddings,

    /// <summary>
    /// POST /v1/moderations. Vacancy checks use a chat JSON prompt, not this endpoint.
    /// </summary>
    ModerationEndpoint,

    /// <summary>Image parts on a chat message. Lobsy sends text only.</summary>
    Vision,

    /// <summary>POST /v1/audio/speech. Voorlezen uses the browser speech API.</summary>
    Tts
}

/// <summary>
/// What the product calls, and what Mistral's chat API accepts.
/// Checked against the Mistral OpenAPI (docs.mistral.ai) on 2026-10-04:
/// chat completions accept <c>json_object</c>, and <c>mistral-small-latest</c> is a current alias.
/// Embeddings, the classifier moderation endpoint, vision payloads and TTS are not called.
/// Mistral does publish those endpoints; we do not send personal data to them.
/// </summary>
public static class AiCapabilities
{
    public static bool IsCalledByLobsy(AiCapability capability)
        => capability is AiCapability.Chat or AiCapability.JsonMode;

    /// <summary>
    /// Chat and JSON mode use the same request shape on Mistral as on OpenAI.
    /// The other capabilities are not called. There is nothing to fall back from.
    /// </summary>
    public static bool WorksOnMistral(AiCapability capability)
        => capability is AiCapability.Chat or AiCapability.JsonMode;
}
