using Jobsy.Core.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>Admin-visible rejection of candidate AI text. The message has no profile text and no PII.</summary>
internal static class AiFactRejectionLog
{
    public const string Category = "ai.facts";

    public static async Task WriteAsync(
        IPlatformErrorLog? platformLog,
        ILogger logger,
        string surface,
        string reason,
        int attempt,
        CancellationToken cancellationToken)
    {
        var safeSurface = CleanToken(surface, "ai");
        var safeReason = CleanToken(reason, "rejected");
        var message = $"AI-tekst afgewezen. surface={safeSurface} reason={safeReason} attempt={attempt}";
        logger.LogWarning("{Message}", message);
        if (platformLog is null)
        {
            return;
        }

        try
        {
            await platformLog.WriteAsync(Category, message, supportCode: null, detail: null, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "AI fact rejection log failed.");
        }
    }

    private static string CleanToken(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var token = value.Trim();
        if (token.Length > 40)
        {
            token = token[..40];
        }

        return token.IndexOfAny(['@', ' ', '\n', '\r']) >= 0 ? fallback : token;
    }
}
