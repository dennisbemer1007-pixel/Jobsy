using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Stub machine-translation: when languages differ, prefixes content so UI/API consumers can verify the path.
/// Replace with a real provider (DeepL, Azure Translator, OpenAI, …) behind the same interface.
/// </summary>
public sealed class TranslationServiceStub : ITranslationService
{
    private readonly ILogger<TranslationServiceStub> _logger;

    public TranslationServiceStub(ILogger<TranslationServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(text ?? string.Empty);
        }

        var source = JobsyLanguages.Normalize(sourceLanguage);
        var target = JobsyLanguages.Normalize(targetLanguage);

        if (JobsyLanguages.AreSame(source, target))
        {
            return Task.FromResult(text);
        }

        _logger.LogDebug(
            "Translation stub {Source}→{Target} ({Length} chars)",
            source, target, text.Length);

        return Task.FromResult($"[{target.ToUpperInvariant()}] {text}");
    }

    public async Task<TranslatedVacancyContent> TranslateVacancyAsync(
        string title,
        string description,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var source = JobsyLanguages.Normalize(sourceLanguage);
        var target = JobsyLanguages.Normalize(targetLanguage);

        if (JobsyLanguages.AreSame(source, target))
        {
            return new TranslatedVacancyContent(title, description, source, target, WasTranslated: false);
        }

        var translatedTitle = await TranslateAsync(title, source, target, cancellationToken);
        var translatedDescription = await TranslateAsync(description, source, target, cancellationToken);

        return new TranslatedVacancyContent(
            translatedTitle,
            translatedDescription,
            source,
            target,
            WasTranslated: true);
    }
}
