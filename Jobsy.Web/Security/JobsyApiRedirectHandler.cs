using Jobsy.Core.Security;

namespace Jobsy.Web.Security;

/// <summary>
/// Follows safe redirects while the inner handler has <c>AllowAutoRedirect = false</c>.
/// Cross-host hops drop the origin secret and other Web→API credentials so a vacancy
/// image redirect (picsum and similar) cannot receive them.
/// </summary>
public sealed class JobsyApiRedirectHandler : DelegatingHandler
{
    public const int MaxRedirects = 10;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var current = request;
        for (var hop = 0; ; hop++)
        {
            var response = await base.SendAsync(current, cancellationToken);
            if (hop >= MaxRedirects
                || (current.Method != HttpMethod.Get && current.Method != HttpMethod.Head)
                || !IsRedirect(response.StatusCode)
                || response.Headers.Location is not { } location
                || current.RequestUri is null)
            {
                return response;
            }

            var nextUri = location.IsAbsoluteUri
                ? location
                : new Uri(current.RequestUri, location);
            if (nextUri.Scheme != Uri.UriSchemeHttp && nextUri.Scheme != Uri.UriSchemeHttps)
            {
                return response;
            }

            var sameEndpoint = string.Equals(
                    current.RequestUri.Host,
                    nextUri.Host,
                    StringComparison.OrdinalIgnoreCase)
                && current.RequestUri.Port == nextUri.Port;

            var next = new HttpRequestMessage(current.Method, nextUri);
            foreach (var header in current.Headers)
            {
                if (!sameEndpoint && IsSensitive(header.Key))
                {
                    continue;
                }

                next.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (!ReferenceEquals(current, request))
            {
                current.Dispose();
            }

            response.Dispose();
            current = next;
        }
    }

    private static bool IsRedirect(System.Net.HttpStatusCode status)
        => status is System.Net.HttpStatusCode.MovedPermanently
            or System.Net.HttpStatusCode.Found
            or System.Net.HttpStatusCode.SeeOther
            or System.Net.HttpStatusCode.TemporaryRedirect
            or System.Net.HttpStatusCode.PermanentRedirect;

    private static bool IsSensitive(string name)
        => name.Equals(CloudflareOriginSecret.HeaderName, StringComparison.OrdinalIgnoreCase)
           || name.Equals(InternalClientIpHeaders.ClientIpHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals(InternalClientIpHeaders.InternalSecretHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
           || name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
           || name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
           || name.StartsWith("X-Jobsy-", StringComparison.OrdinalIgnoreCase);
}
