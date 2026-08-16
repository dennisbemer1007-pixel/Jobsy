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
                if (!retryable || attempt == attempts || !IsTransient(response.StatusCode))
                {
                    return response;
                }

                response.Dispose();
                response = null;
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

            await Task.Delay(200 * attempt, cancellationToken);
        }

        if (last is not null)
        {
            throw last;
        }

        return response ?? throw new InvalidOperationException("API-verzoek gaf geen antwoord.");
    }

    internal static bool IsTransient(HttpStatusCode status)
        => status is HttpStatusCode.Unauthorized
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

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
