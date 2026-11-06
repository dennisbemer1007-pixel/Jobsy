namespace Jobsy.Core.Options;

/// <summary>
/// Which company receives AI calls. Default <c>OpenAI</c>, so a deploy without this
/// setting keeps the current path. Switch with <c>Ai__Provider</c> only.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary><c>OpenAI</c> or <c>Mistral</c>. An unknown name does not call OpenAI.</summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// Optional cheaper model for translation, CV extraction and vacancy moderation.
    /// Used only when the active provider has no small model of its own.
    /// Env: <c>Ai__SmallModel</c>.
    /// </summary>
    public string? SmallModel { get; set; }
}
