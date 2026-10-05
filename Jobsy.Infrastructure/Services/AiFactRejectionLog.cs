using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Enums;
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
        CancellationToken cancellationToken,
        string? model = null,
        string? rejectedText = null)
    {
        var safeSurface = CleanToken(surface, "ai");
        var safeReason = CleanToken(reason, "rejected");
        var safeModel = string.IsNullOrWhiteSpace(model) ? "" : " model=" + CleanToken(model, "model");
        var hash = string.IsNullOrWhiteSpace(rejectedText) ? "" : " textHash=" + TextHash(rejectedText);
        var message = $"AI-tekst afgewezen. surface={safeSurface} reason={safeReason} attempt={attempt}{safeModel}{hash}";
        logger.LogWarning("{Message}", message);
        if (platformLog is null)
        {
            return;
        }

        try
        {
            await platformLog.WriteAsync(
                Category,
                message,
                supportCode: null,
                detail: null,
                PlatformLogLevel.Warning,
                cancellationToken);
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

    private static string TextHash(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
