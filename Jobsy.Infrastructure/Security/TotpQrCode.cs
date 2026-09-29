using System.Text;
using QRCoder;

namespace Jobsy.Infrastructure.Security;

/// <summary>Builds CSP-safe SVG data URIs for authenticator enrollment QR codes.</summary>
public static class TotpQrCode
{
    public static string ToSvgDataUri(string provisioningUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provisioningUri);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(provisioningUri.Trim(), QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(4);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }
}
