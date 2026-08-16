using System.Text.RegularExpressions;
using Jobsy.Core.Email;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Rewrites the remote Lobsy mark in transactional HTML to an inline CID
/// and supplies the small PNG bytes. Mail clients often block or fail
/// remote images; CID shows without a second HTTP fetch.
/// </summary>
internal static class EmailLogoEmbedder
{
    public const string FileName = "lobsy-logo.png";
    public const string ResourceName = "Jobsy.Infrastructure.Assets.lobsy-email.png";

    private static readonly Regex RemoteLogo = new(
        @"https?://[^""'\s>]+/images/brand/lobsy[^""'\s>]*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string RewriteToCid(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html ?? string.Empty;
        }

        return RemoteLogo.Replace(html, "cid:" + EmailLayout.LogoContentId);
    }

    public static byte[] PngBytes()
    {
        var assembly = typeof(EmailLogoEmbedder).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded email logo '{ResourceName}' ontbreekt.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
