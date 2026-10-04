using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>One warning per process when Mistral was asked for but cannot run.</summary>
internal static class AiProviderFallbackLog
{
    private static int _missingKey;
    private static int _unknownProvider;

    public static void MissingMistralKey(ILogger? logger)
    {
        if (logger is null || Interlocked.Exchange(ref _missingKey, 1) != 0)
        {
            return;
        }

        logger.LogWarning(
            "Ai:Provider is Mistral but Mistral:ApiKey is empty. AI calls stay on OpenAI.");
    }

    public static void UnknownProvider(ILogger? logger, string? provider)
    {
        if (logger is null || Interlocked.Exchange(ref _unknownProvider, 1) != 0)
        {
            return;
        }

        logger.LogWarning(
            "Ai:Provider {Provider} is not OpenAI or Mistral. AI calls stay on OpenAI.",
            provider);
    }
}
