namespace Jobsy.Core.Hosting;

/// <summary>
/// Resolves the deployment environment badge label from the public web host (D8).
/// Acceptatie runs as ASPNETCORE_ENVIRONMENT=Production, so that value must not be used.
/// </summary>
public static class DeploymentEnvironment
{
    public const string Acceptatie = "Acceptatie";
    public const string Productie = "Productie";
    public const string Lokaal = "Lokaal";

    public static string Resolve(string? publicWebBaseUrl, string? overrideLabel)
    {
        if (!string.IsNullOrWhiteSpace(overrideLabel))
        {
            return overrideLabel.Trim();
        }

        if (string.IsNullOrWhiteSpace(publicWebBaseUrl))
        {
            return Lokaal;
        }

        if (!Uri.TryCreate(publicWebBaseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return Lokaal;
        }

        var host = uri.Host.Trim().ToLowerInvariant();
        if (host.StartsWith("acceptatie.", StringComparison.Ordinal)
            || host is "acceptatie.lobsy.nl")
        {
            return Acceptatie;
        }

        if (host is "lobsy.nl" or "www.lobsy.nl")
        {
            return Productie;
        }

        return Lokaal;
    }
}
