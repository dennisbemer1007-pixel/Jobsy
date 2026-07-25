namespace Jobsy.Core.Interfaces;

/// <summary>
/// Dynamically translates vacancy (and other user-authored) content between languages.
/// Implementations may call an external MT provider; the default is a stub.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates plain text from <paramref name="sourceLanguage"/> to <paramref name="targetLanguage"/>.
    /// Returns the original text when languages match or the text is empty.
    /// </summary>
    Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Translates vacancy title and description when the candidate language differs from the source language.
    /// </summary>
    Task<TranslatedVacancyContent> TranslateVacancyAsync(
        string title,
        string description,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);
}

public sealed record TranslatedVacancyContent(
    string Title,
    string Description,
    string SourceLanguage,
    string TargetLanguage,
    bool WasTranslated);
