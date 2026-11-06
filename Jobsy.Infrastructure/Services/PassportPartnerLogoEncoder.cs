using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Media;
using SkiaSharp;

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

        SKBitmap bitmap;
        try
        {
            bitmap = SkiaImageCodec.Decode(bytes);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("logo_type");
        }

        using (bitmap)
        {
            if (bitmap.Width < PassportPartnerLogoRules.MinWidthPx)
            {
                throw new InvalidOperationException("logo_too_narrow");
            }

            return SkiaImageCodec.EncodePng(bitmap);
        }
    }
}
