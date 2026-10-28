using SkiaSharp;

namespace Jobsy.Infrastructure.Media;

/// <summary>MIT-licensed image decode/encode/resize (replaces ImageSharp in production).</summary>
public static class SkiaImageCodec
{
    public static SKBitmap Decode(ReadOnlySpan<byte> bytes)
    {
        var bitmap = SKBitmap.Decode(bytes);
        if (bitmap is null)
        {
            throw new InvalidOperationException("image_decode_failed");
        }

        return bitmap;
    }

    public static byte[] EncodePng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray() ?? throw new InvalidOperationException("png_encode_failed");
    }

    public static byte[] EncodeWebp(SKBitmap bitmap, int quality)
    {
        quality = Math.Clamp(quality, 1, 100);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        return data?.ToArray() ?? throw new InvalidOperationException("webp_encode_failed");
    }

    public static SKBitmap ResizeToMaxExtent(SKBitmap source, int maxExtent)
    {
        if (source.Width <= maxExtent && source.Height <= maxExtent)
        {
            return source.Copy();
        }

        var scale = Math.Min(maxExtent / (float)source.Width, maxExtent / (float)source.Height);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var info = new SKImageInfo(width, height, source.ColorType, source.AlphaType);
        var resized = new SKBitmap(info);
        if (!source.ScalePixels(resized, SKSamplingOptions.Default))
        {
            resized.Dispose();
            throw new InvalidOperationException("resize_failed");
        }

        return resized;
    }

    public static SKBitmap ResizeMaxWidth(SKBitmap source, int targetWidth)
    {
        if (source.Width <= targetWidth)
        {
            return source.Copy();
        }

        var scale = targetWidth / (float)source.Width;
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var info = new SKImageInfo(targetWidth, height, source.ColorType, source.AlphaType);
        var resized = new SKBitmap(info);
        if (!source.ScalePixels(resized, SKSamplingOptions.Default))
        {
            resized.Dispose();
            throw new InvalidOperationException("resize_failed");
        }

        return resized;
    }

    public static SKBitmap CreateSolid(int width, int height, SKColor color)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        bitmap.Erase(color);
        return bitmap;
    }

    public static SKBitmap ApplyOpacity(SKBitmap source, float opacity)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        var copy = source.Copy();
        for (var y = 0; y < copy.Height; y++)
        {
            for (var x = 0; x < copy.Width; x++)
            {
                var c = copy.GetPixel(x, y);
                copy.SetPixel(
                    x,
                    y,
                    new SKColor(
                        c.Red,
                        c.Green,
                        c.Blue,
                        (byte)Math.Round(c.Alpha * opacity)));
            }
        }

        return copy;
    }

    public static SKColor ParseHexColor(string? hex, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
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
                return new SKColor(
                    (byte)((raw >> 16) & 0xFF),
                    (byte)((raw >> 8) & 0xFF),
                    (byte)(raw & 0xFF));
            }

            return new SKColor(
                (byte)((raw >> 24) & 0xFF),
                (byte)((raw >> 16) & 0xFF),
                (byte)((raw >> 8) & 0xFF),
                (byte)(raw & 0xFF));
        }

        return fallback;
    }
}
