namespace Jobsy.Core.Legal;

/// <summary>
/// Third-party hosts and NuGet package names that must have a row in <see cref="LegalProcessors"/>.
/// The unit test scans package references and configured base URLs for these tokens.
/// </summary>
public static class LegalThirdPartyWatch
{
    public static readonly IReadOnlyList<(string HostSuffix, string ProcessorId)> Hosts =
    [
        ("resend.com", "resend"),
        ("lettermint.co", "lettermint"),
        ("openai.com", "openai"),
        ("sentry.io", "sentry"),
        ("mollie.com", "mollie"),
        ("cloudflare.com", "cloudflare"),
        ("scaleway.com", "scaleway"),
        ("mistral.ai", "mistral")
    ];

    public static readonly IReadOnlyList<(string PackageSegment, string ProcessorId)> Packages =
    [
        ("Resend", "resend"),
        ("Lettermint", "lettermint"),
        ("OpenAI", "openai"),
        ("Sentry", "sentry"),
        ("Mollie", "mollie"),
        ("Cloudflare", "cloudflare"),
        ("Scaleway", "scaleway"),
        ("Mistral", "mistral")
    ];

    public static IEnumerable<string> ProcessorIdsForHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            yield break;
        }

        var trimmed = host.Trim().TrimEnd('.');
        foreach (var (suffix, processorId) in Hosts)
        {
            if (trimmed.Equals(suffix, StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
            {
                yield return processorId;
            }
        }
    }

    public static IEnumerable<string> ProcessorIdsForPackage(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            yield break;
        }

        var segments = packageId.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var (segment, processorId) in Packages)
        {
            if (segments.Any(part => part.Equals(segment, StringComparison.OrdinalIgnoreCase)))
            {
                yield return processorId;
            }
        }
    }
}
