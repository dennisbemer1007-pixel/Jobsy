using System.Net;
using System.Text.Json;

namespace Jobsy.Web.Scholen;

/// <summary>
/// First load of a pupil page. A real 401/403 means the code session is gone.
/// A network blip, 5xx or circuit hiccup is retried, then shown as offline.
/// HTTP 429 waits briefly (honouring Retry-After when present), then shows a wait message — never "Niet bewaard".
/// </summary>
public static class PupilLoadRetry
{
    /// <summary>First try immediately, then 300 ms, 1 s, and one extra 2 s try before the offline card.</summary>
    public static readonly int[] AttemptDelaysMs = [0, 300, 1000, 2000];

    /// <summary>Cap a single 429 wait so the SSR page does not hang on a full 60 s API window.</summary>
    public static readonly TimeSpan MaxRateLimitWait = TimeSpan.FromSeconds(3);

    public static bool IsSessionLost(Exception exception)
        => exception is HttpRequestException http && IsSessionLost(http.StatusCode);

    public static bool IsSessionLost(HttpStatusCode? status)
        => status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    public static bool IsRateLimited(HttpStatusCode? status)
        => status is HttpStatusCode.TooManyRequests;

    public static Task<PupilLoadOutcome<T>> RunAsync<T>(
        Func<Task<T?>> fetch,
        CancellationToken cancellationToken = default) where T : class
        => RunAsync(fetch, services: null, operation: "load", cancellationToken);

    public static async Task<PupilLoadOutcome<T>> RunAsync<T>(
        Func<Task<T?>> fetch,
        IServiceProvider? services,
        string operation,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(fetch);
        Exception? lastError = null;
        HttpStatusCode? lastStatus = null;
        for (var attempt = 0; attempt < AttemptDelaysMs.Length; attempt++)
        {
            if (AttemptDelaysMs[attempt] > 0)
            {
                await Task.Delay(AttemptDelaysMs[attempt], cancellationToken);
            }

            try
            {
                var value = await fetch();
                if (value is not null)
                {
                    return PupilLoadOutcome<T>.Ok(value);
                }

                lastError = null;
                lastStatus = null;
                LeerlingLoadLog.Failed(services, operation, null, null, giveUp: false);
            }
            catch (Exception ex) when (IsSessionLost(ex))
            {
                return PupilLoadOutcome<T>.Lost();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException http && IsRateLimited(http.StatusCode))
            {
                lastError = ex;
                lastStatus = http.StatusCode;
                LeerlingLoadLog.Failed(services, operation, ex, lastStatus, giveUp: false);
                var wait = ResolveRateLimitWait(http);
                if (wait > TimeSpan.Zero && attempt < AttemptDelaysMs.Length - 1)
                {
                    await Task.Delay(wait, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
                lastStatus = ex is HttpRequestException http ? http.StatusCode : null;
                LeerlingLoadLog.Failed(services, operation, ex, lastStatus, giveUp: false);
            }
        }

        LeerlingLoadLog.Failed(services, operation, lastError, lastStatus, giveUp: true);
        if (IsRateLimited(lastStatus))
        {
            return PupilLoadOutcome<T>.RateLimited();
        }

        return PupilLoadOutcome<T>.Offline();
    }

    public static TimeSpan ResolveRateLimitWait(HttpRequestException http)
    {
        var seconds = TryParseRetryAfterSeconds(http);
        if (seconds is null or <= 0)
        {
            return TimeSpan.FromMilliseconds(400);
        }

        var wait = TimeSpan.FromSeconds(seconds.Value);
        return wait > MaxRateLimitWait ? MaxRateLimitWait : wait;
    }

    public static int? TryParseRetryAfterSeconds(HttpRequestException http)
    {
        if (!IsRateLimited(http.StatusCode))
        {
            return null;
        }

        var message = http.Message;
        var jsonStart = message.IndexOf('{', StringComparison.Ordinal);
        if (jsonStart >= 0)
        {
            try
            {
                using var doc = JsonDocument.Parse(message[jsonStart..]);
                if (doc.RootElement.TryGetProperty("retryAfterSeconds", out var camel)
                    && camel.TryGetInt32(out var fromCamel))
                {
                    return fromCamel;
                }

                if (doc.RootElement.TryGetProperty("RetryAfterSeconds", out var pascal)
                    && pascal.TryGetInt32(out var fromPascal))
                {
                    return fromPascal;
                }
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        return null;
    }
}

public readonly record struct PupilLoadOutcome<T> where T : class
{
    public T? Value { get; private init; }

    public bool SessionLost { get; private init; }

    public bool IsOffline { get; private init; }

    public bool IsRateLimited { get; private init; }

    public static PupilLoadOutcome<T> Ok(T value) => new() { Value = value };

    public static PupilLoadOutcome<T> Lost() => new() { SessionLost = true };

    public static PupilLoadOutcome<T> Offline() => new() { IsOffline = true };

    public static PupilLoadOutcome<T> RateLimited() => new() { IsRateLimited = true };
}
