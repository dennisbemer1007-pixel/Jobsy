using Jobsy.Core.Rules;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace Jobsy.Infrastructure.Services;

public static class PassportPartnerLogoEncoder
{
    public static byte[] ReencodePng(byte[] bytes, string? fileName, string? contentType)
    {
        var reason = PassportPartnerLogoRules.RejectReason(fileName, contentType, bytes.Length);
        if (reason is not null)
        {
            throw new InvalidOperationException(reason);
        }

        if (bytes.Length > 0 && bytes[0] == (byte)'<')
        {
            throw new InvalidOperationException("logo_svg");
        }

        Image image;
        try
        {
            image = Image.Load(bytes);
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidOperationException("logo_type");
        }

        using (image)
        {
            if (image.Width < PassportPartnerLogoRules.MinWidthPx)
            {
                throw new InvalidOperationException("logo_too_narrow");
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;
            using var output = new MemoryStream();
            image.Save(output, new PngEncoder());
            return output.ToArray();
        }
    }
}
