namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Resolves the KVK Handelsregister API root. Admin Base URL is optional; an empty value
/// uses production. Common paste mistakes (zoeken-URL, developer portal, missing /api)
/// are normalized so live calls hit <c>.../api/v1/...</c> and <c>.../api/v2/zoeken</c>.
/// </summary>
public static class KvkApiBaseUrl
{
    public const string Production = "https://api.kvk.nl/api/";
    public const string Test = "https://api.kvk.nl/test/api/";

    private static readonly string[] ResourceSuffixes =
    [
        "/v2/zoeken",
        "/v1/basisprofielen",
        "/v1/vestigingsprofielen",
        "/v1/naamgevingen"
    ];

    public static string Resolve(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return Production;
        }

        if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(configured, out var normalized, out _)
            || string.IsNullOrWhiteSpace(normalized))
        {
            return Production;
        }

        var uri = new Uri(normalized);
        if (uri.Host.Equals("developers.kvk.nl", StringComparison.OrdinalIgnoreCase))
        {
            return Production;
        }

        if (!uri.Host.Equals("api.kvk.nl", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        foreach (var suffix in ResourceSuffixes)
        {
            if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                path = path[..^suffix.Length];
                break;
            }
        }

        path = path.TrimEnd('/');
        var isTest = path.Equals("/test", StringComparison.OrdinalIgnoreCase)
                     || path.StartsWith("/test/", StringComparison.OrdinalIgnoreCase)
                     || path.Equals("/test/api", StringComparison.OrdinalIgnoreCase);

        return isTest ? Test : Production;
    }

    public static string EnvironmentLabel(string baseUrl)
        => baseUrl.Contains("/test/", StringComparison.OrdinalIgnoreCase)
            ? "testomgeving"
            : "productie";
}
