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

    public static string Resolve(string? publicWebBaseUrl, string? overrideLabel, string? requestHost = null)
    {
        var fromOverride = CanonicalLabel(overrideLabel);
        if (fromOverride is not null)
        {
            return fromOverride;
        }

        if (!string.IsNullOrWhiteSpace(publicWebBaseUrl))
        {
            return FromUrl(publicWebBaseUrl) ?? Lokaal;
        }

        return FromHost(requestHost) ?? Lokaal;
    }

    /// <summary>
    /// Maps dashboard values (<c>Production</c>) and the Dutch badge labels onto
    /// <see cref="Acceptatie"/>, <see cref="Productie"/> and <see cref="Lokaal"/>.
    /// An unknown non-empty override is kept as typed.
    /// </summary>
    private static string? CanonicalLabel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Equals(Acceptatie, StringComparison.OrdinalIgnoreCase))
        {
            return Acceptatie;
        }

        if (trimmed.Equals(Productie, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            return Productie;
        }

        if (trimmed.Equals(Lokaal, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            return Lokaal;
        }

        return trimmed;
    }

    private static string? FromUrl(string publicWebBaseUrl)
    {
        if (!Uri.TryCreate(publicWebBaseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return Lokaal;
        }

        return FromHost(uri.Host) ?? Lokaal;
    }

    private static string? FromHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var value = host.Trim().ToLowerInvariant();
        if (value.StartsWith("acceptatie.", StringComparison.Ordinal)
            || value is "acceptatie.lobsy.nl"
            || (value.StartsWith("lobsy-acc-", StringComparison.Ordinal)
                && value.EndsWith(".onrender.com", StringComparison.Ordinal)))
        {
            return Acceptatie;
        }

        if (value is "lobsy.nl" or "www.lobsy.nl")
        {
            return Productie;
        }

        return null;
    }
}
