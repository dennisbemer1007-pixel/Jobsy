using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Infrastructure.Media;

/// <summary>
/// Small WebP thumbnails for banenkaart map popups (load on open, not with pins/list).
/// </summary>
public static class VacancyMapPopupPhotoEncoder
{
    public const int DesktopWidth = 640;
    public const int MobileWidth = 248;
    public const int Quality = 70;

    public static int WidthForVariant(string? variant)
        => string.Equals(variant, "mobile", StringComparison.OrdinalIgnoreCase)
            ? MobileWidth
            : DesktopWidth;

    public static byte[] ToWebp(ReadOnlySpan<byte> sourceBytes, int targetWidth)
    {
        using var image = Image.Load(sourceBytes);
        if (image.Width > targetWidth)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, 0),
                Mode = ResizeMode.Max
            }));
        }

        using var ms = new MemoryStream();
        image.SaveAsWebp(ms, new WebpEncoder { Quality = Quality });
        return ms.ToArray();
    }

    /// <summary>Category-colour tile when no photo bytes exist on disk (acc / missing uploads).</summary>
    public static byte[] CategoryPlaceholderWebp(string? categoryColorHex, int targetWidth, string? variant = null)
    {
        var width = targetWidth > 0 ? targetWidth : DesktopWidth;
        var height = string.Equals(variant, "mobile", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, (int)Math.Round(width * 9.0 / 16.0))
            : Math.Max(1, (int)Math.Round(width * 360.0 / 640.0));
        var rgba = ParseCategoryColor(categoryColorHex);
        using var image = new Image<Rgba32>(width, height, rgba);
        using var ms = new MemoryStream();
        image.SaveAsWebp(ms, new WebpEncoder { Quality = Quality });
        return ms.ToArray();
    }

    private static Rgba32 ParseCategoryColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return new Rgba32(0x33, 0x66, 0xcc, 255);
        }

        var s = hex.Trim();
        if (s.StartsWith('#'))
        {
            s = s[1..];
        }

        if (s.Length is 6 or 8 && uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var raw))
        {
            if (s.Length == 6)
            {
                return new Rgba32(
                    (byte)((raw >> 16) & 0xFF),
                    (byte)((raw >> 8) & 0xFF),
                    (byte)(raw & 0xFF),
                    255);
            }

            return new Rgba32(
                (byte)((raw >> 24) & 0xFF),
                (byte)((raw >> 16) & 0xFF),
                (byte)((raw >> 8) & 0xFF),
                (byte)(raw & 0xFF));
        }

        return new Rgba32(0x33, 0x66, 0xcc, 255);
    }
}
