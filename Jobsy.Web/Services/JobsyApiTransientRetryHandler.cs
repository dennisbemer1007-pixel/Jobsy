using System.Globalization;
using System.Net;

namespace Jobsy.Web.Services;

/// <summary>
/// Retries safe GETs when the Blazor circuit is still settling auth or the API
/// is briefly unavailable. Prevents a flash of error on /home after login.
/// Outer handler so each attempt re-runs <see cref="JobsyApiAuthHandler"/>.
/// </summary>
public sealed class JobsyApiTransientRetryHandler : DelegatingHandler
{
    public const int MaxAttempts = 3;
    public static readonly TimeSpan MaxRetryAfter = TimeSpan.FromMilliseconds(1500);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var retryable = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;
        var attempts = retryable ? MaxAttempts : 1;
        HttpResponseMessage? response = null;
        Exception? last = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            last = null;
            response?.Dispose();
            response = null;

            var outgoing = attempt == 1 ? request : CloneForRetry(request);

            try
            {
                response = await base.SendAsync(outgoing, cancellationToken);
                if (!retryable || attempt == attempts || !ShouldRetry(response.StatusCode, outgoing))
                {
                    return response;
                }

                var delay = ResolveDelay(response, attempt);
                response.Dispose();
                response = null;
                await Task.Delay(delay, cancellationToken);
                continue;
            }
            catch (HttpRequestException ex) when (retryable && attempt < attempts)
            {
                last = ex;
            }
            catch (TaskCanceledException ex) when (
                retryable
                && attempt < attempts
                && !cancellationToken.IsCancellationRequested)
            {
                last = ex;
            }

            await Task.Delay(ResolveDelay(null, attempt), cancellationToken);
        }

        if (last is not null)
        {
            throw last;
        }

        return response ?? throw new InvalidOperationException("API-verzoek gaf geen antwoord.");
    }

    public static bool IsTransient(HttpStatusCode status)
        => status is HttpStatusCode.Unauthorized
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            || ((int)status >= 500 && (int)status <= 599);

    /// <summary>
    /// 401 is only retried when the attempt went out without credentials
    /// (circuit still settling). An authenticated 401 is a real denial.
    /// 429 / 5xx: one retry for GET (loop capped by <see cref="MaxAttempts"/>).
    /// </summary>
    public static bool ShouldRetry(HttpStatusCode status, HttpRequestMessage sent)
    {
        if (status == HttpStatusCode.Unauthorized)
        {
            return !HasAttachedAuth(sent);
        }

        return IsTransient(status);
    }

    public static TimeSpan ResolveDelay(HttpResponseMessage? response, int attempt)
    {
        if (response?.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ParseRetryAfter(response);
            var capped = retryAfter > MaxRetryAfter ? MaxRetryAfter : retryAfter;
            if (capped <= TimeSpan.Zero)
            {
                capped = TimeSpan.FromMilliseconds(400);
            }

            // Small jitter so concurrent tabs do not stampede.
            var jitterMs = Random.Shared.Next(50, 200);
            return capped + TimeSpan.FromMilliseconds(jitterMs);
        }

        return TimeSpan.FromMilliseconds(200 * attempt);
    }

    public static TimeSpan ParseRetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        if (header is null)
        {
            return TimeSpan.FromMilliseconds(400);
        }

        if (header.Delta is TimeSpan delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (header.Date is DateTimeOffset date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
        }

        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var raw = values.FirstOrDefault();
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                && seconds >= 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }
        }

        return TimeSpan.FromMilliseconds(400);
    }

    private static bool HasAttachedAuth(HttpRequestMessage request)
        => request.Headers.Contains("X-Jobsy-Email")
           || request.Headers.Authorization is not null;

    /// <summary>
    /// Fresh request so <see cref="JobsyApiAuthHandler"/> can attach the identity
    /// that became available after the first 401.
    /// </summary>
    internal static HttpRequestMessage CloneForRetry(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            if (IsAuthOrDerivedHeader(header.Key))
            {
                continue;
            }

            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }

    private static bool IsAuthOrDerivedHeader(string key)
        => key.StartsWith("X-Jobsy-", StringComparison.OrdinalIgnoreCase)
           || key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
           || key.Equals("Accept", StringComparison.OrdinalIgnoreCase);
}
