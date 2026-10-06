using Jobsy.Core;
using Jobsy.Core.Security;

namespace Jobsy.Web.Security;

/// <summary>
/// Adds <see cref="CloudflareOriginMiddleware.HeaderName"/> on server-side calls to the
/// Jobsy API when <c>CLOUDFLARE_ORIGIN_SECRET</c> is set. Other hosts (Nominatim, PDOK,
/// third-party image redirects) never receive the secret.
/// </summary>
public sealed class CloudflareOriginHeaderHandler : DelegatingHandler
{
    private readonly string? _secret;
    private readonly HashSet<string> _apiEndpoints;

    public CloudflareOriginHeaderHandler(IConfiguration configuration)
    {
        _secret = CloudflareOriginSecret.Normalize(
            configuration[CloudflareOriginMiddleware.ConfigKey]
            ?? configuration[CloudflareOriginMiddleware.ConfigKeyAlt]);
        _apiEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddEndpoint(_apiEndpoints, configuration["ApiBaseUrl"], useLocalFallback: true);
        AddEndpoint(_apiEndpoints, configuration["JobsyApi:BaseUrl"], useLocalFallback: false);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_secret is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        if (TargetsApi(request.RequestUri))
        {
            request.Headers.Remove(CloudflareOriginMiddleware.HeaderName);
            request.Headers.TryAddWithoutValidation(CloudflareOriginMiddleware.HeaderName, _secret);
        }
        else
        {
            // A copied browser header must not leave the process toward a third party.
            request.Headers.Remove(CloudflareOriginMiddleware.HeaderName);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private bool TargetsApi(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            return false;
        }

        return _apiEndpoints.Contains(EndpointKey(uri));
    }

    private static void AddEndpoint(HashSet<string> endpoints, string? raw, bool useLocalFallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (!useLocalFallback)
            {
                return;
            }

            raw = "http://localhost:5200/";
        }

        var origin = JobsyPublicUrl.NormalizeOrigin(raw);
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return;
        }

        endpoints.Add(EndpointKey(uri));
    }

    internal static string EndpointKey(Uri uri)
        => uri.Host + ":" + uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
