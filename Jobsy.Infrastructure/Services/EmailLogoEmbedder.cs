using System.Text.RegularExpressions;
using MimeKit;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Inlines the compact Lobsy mark for SMTP (Outlook). Resend keeps the hosted HTTPS URL
/// because Gmail/mobile webmail do not render CID images.
/// </summary>
internal static class EmailLogoEmbedder
{
    public const string ContentId = "lobsy-logo@lobsy.nl";
    public const string ResourceName = "Jobsy.Infrastructure.Assets.lobsy-email.png";

    private static readonly Regex HostedLogoUrl = new(
        @"https?://[^""'\s>]+/images/brand/lobsy(?:-email|-128)?\.png(?:\?[^""'\s>]*)?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static byte[] LoadPng()
    {
        var assembly = typeof(EmailLogoEmbedder).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded email logo '{ResourceName}' ontbreekt.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static string WithCidLogo(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html ?? string.Empty;
        }

        return HostedLogoUrl.Replace(html, "cid:" + ContentId);
    }

    public static void AddInlineLogo(BodyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var part = builder.LinkedResources.Add(
            "lobsy-email.png",
            LoadPng(),
            new ContentType("image", "png"));
        part.ContentId = ContentId;
        part.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
    }
}
