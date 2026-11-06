using SkiaSharp;

namespace Jobsy.Infrastructure.Media;

/// <summary>
/// Small WebP thumbnails for banenkaart map popups (load on open, not with pins/list).
/// </summary>
public static class VacancyMapPopupPhotoEncoder
{
    public const int DesktopWidth = 640;
    public const int MobileWidth = 248;
    public const int Quality = 70;
    public const int DesktopQuality = 75;
    public const int MobileQuality = 62;

    public static int WidthForVariant(string? variant)
        => string.Equals(variant, "mobile", StringComparison.OrdinalIgnoreCase)
            ? MobileWidth
            : DesktopWidth;

    public static int QualityForVariant(string? variant)
        => string.Equals(variant, "mobile", StringComparison.OrdinalIgnoreCase)
            ? MobileQuality
            : DesktopQuality;

    public static byte[] ToWebp(ReadOnlySpan<byte> sourceBytes, int targetWidth, string? variant = null)
    {
        using var decoded = SkiaImageCodec.Decode(sourceBytes);
        using var resized = decoded.Width > targetWidth
            ? SkiaImageCodec.ResizeMaxWidth(decoded, targetWidth)
            : decoded.Copy();
        var quality = variant is null ? Quality : QualityForVariant(variant);
        return SkiaImageCodec.EncodeWebp(resized, quality);
    }

    /// <summary>Category-colour tile when no photo bytes exist on disk (acc / missing uploads).</summary>
    public static byte[] CategoryPlaceholderWebp(string? categoryColorHex, int targetWidth, string? variant = null)
    {
        var width = targetWidth > 0 ? targetWidth : DesktopWidth;
        var height = string.Equals(variant, "mobile", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, (int)Math.Round(width * 9.0 / 16.0))
            : Math.Max(1, (int)Math.Round(width * 360.0 / 640.0));
        var rgba = SkiaImageCodec.ParseHexColor(categoryColorHex, new SKColor(0x33, 0x66, 0xcc));
        using var image = SkiaImageCodec.CreateSolid(width, height, rgba);
        return SkiaImageCodec.EncodeWebp(image, Quality);
    }
}
