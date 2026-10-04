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
}
