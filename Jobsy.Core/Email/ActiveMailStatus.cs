namespace Jobsy.Core.Email;

/// <summary>What the admin integrations page shows for the mail company that actually sends.</summary>
public static class ActiveMailStatus
{
    public const string RegionNl = "nl";
    public const string RegionUs = "us";
    public const string RegionOff = "off";

    public static MailRuntimeStatus Describe(string? provider, bool lettermintApiKeyConfigured, string? lettermintBaseUrl)
    {
        var choice = MailProviderChoice.Choose(provider, lettermintApiKeyConfigured);
        if (choice.Kind == MailProviderKind.Lettermint)
        {
            return new MailRuntimeStatus(
                MailProviderNames.Lettermint,
                RegionNl,
                HostOf(string.IsNullOrWhiteSpace(lettermintBaseUrl)
                    ? "https://api.lettermint.co/v1/"
                    : lettermintBaseUrl),
                Available: true);
        }

        if (choice.Kind == MailProviderKind.Resend)
        {
            return new MailRuntimeStatus(
                MailProviderNames.Resend,
                RegionUs,
                "api.resend.com",
                Available: true);
        }

        return new MailRuntimeStatus(MailProviderNames.Lettermint, RegionOff, null, Available: false);
    }

    private static string? HostOf(string? baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Host;
    }
}

public sealed record MailRuntimeStatus(
    string Provider,
    string RegionCode,
    string? EndpointHost,
    bool Available);
