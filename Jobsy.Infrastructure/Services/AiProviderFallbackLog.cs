using Jobsy.Core.Ai;
using Jobsy.Core.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>One error per process when the chosen AI provider cannot run.</summary>
internal static class AiProviderFallbackLog
{
    private static int _missingKey;
    private static int _unknownProvider;

    public static async Task ReportUnavailableAsync(
        ILogger? logger,
        IPlatformErrorLog? platformLog,
        AiProviderDecision decision,
        string? provider,
        CancellationToken cancellationToken = default)
    {
        if (decision.RequestedMistralWithoutKey)
        {
            if (Interlocked.Exchange(ref _missingKey, 1) != 0)
            {
                return;
            }

            const string message =
                "AI staat uit. Ai:Provider is Mistral, maar de Mistral-sleutel ontbreekt of is ongeldig. OpenAI wordt niet gebruikt.";
            logger?.LogError(message);
            if (platformLog is not null)
            {
                await platformLog.WriteAsync("AI", message, supportCode: null, detail: null, cancellationToken);
            }

            return;
        }

        if (decision.UnknownProvider)
        {
            if (Interlocked.Exchange(ref _unknownProvider, 1) != 0)
            {
                return;
            }

            var shown = string.IsNullOrWhiteSpace(provider) ? "(leeg)" : provider.Trim();
            logger?.LogError(
                "AI staat uit. Ai:Provider '{Provider}' is niet OpenAI of Mistral. OpenAI wordt niet gebruikt.",
                shown);
            var message =
                $"AI staat uit. Ai:Provider '{shown}' is niet OpenAI of Mistral. OpenAI wordt niet gebruikt.";
            if (platformLog is not null)
            {
                await platformLog.WriteAsync("AI", message, supportCode: null, detail: null, cancellationToken);
            }
        }
    }

    internal static void ResetForTests()
    {
        Interlocked.Exchange(ref _missingKey, 0);
        Interlocked.Exchange(ref _unknownProvider, 0);
    }
}
