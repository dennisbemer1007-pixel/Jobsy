using Jobsy.Core.Options;

namespace Jobsy.Core.Email;

/// <summary>Resolved brand assets and legal footer for the renderer.</summary>
public sealed record EmailBrand(
    string PublicWebBaseUrl,
    string LogoUrl,
    string MascotUrl,
    string SupportAddress,
    string LegalName,
    string LegalAddress,
    string KvkNumber,
    string AssetVersion)
{
    public string LegalLine
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(LegalName))
            {
                parts.Add(LegalName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(LegalAddress))
            {
                parts.Add(LegalAddress.Trim());
            }

            if (!string.IsNullOrWhiteSpace(KvkNumber))
            {
                parts.Add("KvK " + KvkNumber.Trim());
            }

            return string.Join(" · ", parts);
        }
    }

    public static EmailBrand From(MailOptions options, string publicWebBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(options);
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(
            string.IsNullOrWhiteSpace(publicWebBaseUrl) ? "http://localhost:5201" : publicWebBaseUrl);
        var version = string.IsNullOrWhiteSpace(options.AssetVersion)
            ? MailOptions.DefaultAssetVersion
            : options.AssetVersion.Trim();
        var support = string.IsNullOrWhiteSpace(options.SupportAddress)
            ? MailOptions.DefaultSupportAddress
            : options.SupportAddress.Trim();
        var legalName = string.IsNullOrWhiteSpace(options.LegalName)
            ? MailOptions.DefaultLegalName
            : options.LegalName.Trim();

        return new EmailBrand(
            PublicWebBaseUrl: baseUrl,
            LogoUrl: $"{baseUrl}/images/email/lobsy-mark-72.png?v={version}",
            MascotUrl: $"{baseUrl}/images/email/mascot-celebrating-128.png?v={version}",
            SupportAddress: support,
            LegalName: legalName,
            LegalAddress: options.LegalAddress?.Trim() ?? string.Empty,
            KvkNumber: options.KvkNumber?.Trim() ?? string.Empty,
            AssetVersion: version);
    }

    /// <summary>Test/preview helper with production-like defaults.</summary>
    public static EmailBrand ForBaseUrl(string publicWebBaseUrl)
        => From(new MailOptions(), publicWebBaseUrl);
}
