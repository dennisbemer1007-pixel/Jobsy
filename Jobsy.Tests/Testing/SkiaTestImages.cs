using SkiaSharp;

namespace Jobsy.Tests.Testing;

public readonly record struct TestRgb(byte R, byte G, byte B)
{
    public static implicit operator TestRgb((byte R, byte G, byte B) t) => new(t.R, t.G, t.B);
}

public readonly record struct TestRgba(byte R, byte G, byte B, byte A)
{
    public static implicit operator TestRgba((byte R, byte G, byte B, byte A) t) => new(t.R, t.G, t.B, t.A);
}

/// <summary>Test-only pixel access (replaces ImageSharp in screenshot/pixel tests).</summary>
public sealed class SkiaTestImage : IDisposable
{
    private readonly SKBitmap _bitmap;

    private SkiaTestImage(SKBitmap bitmap) => _bitmap = bitmap;

    public int Width => _bitmap.Width;

    public int Height => _bitmap.Height;

    public (byte R, byte G, byte B) GetRgb(int x, int y)
    {
        var c = _bitmap.GetPixel(x, y);
        return (c.Red, c.Green, c.Blue);
    }

    public (byte R, byte G, byte B, byte A) GetRgba(int x, int y)
    {
        var c = _bitmap.GetPixel(x, y);
        return (c.Red, c.Green, c.Blue, c.Alpha);
    }

    public static SkiaTestImage LoadRgb(string absolutePath)
    {
        using var stream = File.OpenRead(absolutePath);
        var decoded = SKBitmap.Decode(stream) ?? throw new InvalidOperationException($"Could not decode {absolutePath}");
        var rgb = decoded.Copy(SKColorType.Rgb888x) ?? decoded.Copy();
        decoded.Dispose();
        return new SkiaTestImage(rgb);
    }

    public static SkiaTestImage LoadRgba(string absolutePath)
    {
        using var stream = File.OpenRead(absolutePath);
        var decoded = SKBitmap.Decode(stream) ?? throw new InvalidOperationException($"Could not decode {absolutePath}");
        return new SkiaTestImage(decoded);
    }

    public static SkiaTestImage LoadRgb(ReadOnlySpan<byte> bytes)
    {
        var decoded = SKBitmap.Decode(bytes) ?? throw new InvalidOperationException("Could not decode image bytes");
        var rgb = decoded.Copy(SKColorType.Rgb888x) ?? decoded.Copy();
        decoded.Dispose();
        return new SkiaTestImage(rgb);
    }

    public static SkiaTestImage LoadRgba(ReadOnlySpan<byte> bytes)
    {
        var decoded = SKBitmap.Decode(bytes) ?? throw new InvalidOperationException("Could not decode image bytes");
        return new SkiaTestImage(decoded);
    }

    public static byte[] EncodePng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray() ?? throw new InvalidOperationException("png_encode_failed");
    }

    public static void SavePng(SKBitmap bitmap, string absolutePath)
    {
        File.WriteAllBytes(absolutePath, EncodePng(bitmap));
    }

    public static Task SavePngAsync(SKBitmap bitmap, string absolutePath) =>
        File.WriteAllBytesAsync(absolutePath, EncodePng(bitmap));

    /// <summary>Scale to fit inside target size, letterboxing with <paramref name="padColor"/>.</summary>
    public static SKBitmap ResizePad(SKBitmap source, int targetWidth, int targetHeight, SKColor padColor)
    {
        var scale = Math.Min(targetWidth / (float)source.Width, targetHeight / (float)source.Height);
        var scaledW = Math.Max(1, (int)Math.Round(source.Width * scale));
        var scaledH = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var scaled = Resize(source, scaledW, scaledH);
        var canvasBmp = CreateSolid(targetWidth, targetHeight, padColor);
        using var canvas = new SKCanvas(canvasBmp);
        canvas.DrawBitmap(scaled, (targetWidth - scaledW) / 2f, (targetHeight - scaledH) / 2f);
        return canvasBmp;
    }

    public static SKBitmap ComposeHorizontal(
        IReadOnlyList<SKBitmap> images,
        int gap,
        SKColor background,
        int topPadding = 0)
    {
        var totalW = images.Sum(b => b.Width) + gap * Math.Max(0, images.Count - 1);
        var maxH = images.Max(b => b.Height);
        var canvasBmp = CreateSolid(totalW, maxH + topPadding, background);
        using var canvas = new SKCanvas(canvasBmp);
        var x = 0;
        foreach (var img in images)
        {
            canvas.DrawBitmap(img, x, topPadding + (maxH - img.Height) / 2f);
            x += img.Width + gap;
        }

        return canvasBmp;
    }

    public static SKBitmap Crop(SKBitmap source, int x, int y, int width, int height)
    {
        var info = new SKImageInfo(width, height, source.ColorType, source.AlphaType);
        var dest = new SKBitmap(info);
        using var canvas = new SKCanvas(dest);
        canvas.DrawBitmap(source, -x, -y);
        return dest;
    }

    public static SKBitmap ResizeMax(SKBitmap source, int maxWidth, int maxHeight)
    {
        var scale = Math.Min(maxWidth / (float)source.Width, maxHeight / (float)source.Height);
        if (scale >= 1f)
        {
            return source.Copy();
        }

        var w = Math.Max(1, (int)Math.Round(source.Width * scale));
        var h = Math.Max(1, (int)Math.Round(source.Height * scale));
        return Resize(source, w, h);
    }

    public static byte[] EncodeJpeg(SKBitmap bitmap, int quality = 85)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, Math.Clamp(quality, 1, 100));
        return data?.ToArray() ?? throw new InvalidOperationException("jpeg_encode_failed");
    }

    public static SKBitmap CreateSolid(int width, int height, SKColor color)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        bitmap.Erase(color);
        return bitmap;
    }

    public static SKBitmap Resize(SKBitmap source, int width, int height)
    {
        var info = new SKImageInfo(width, height, source.ColorType, source.AlphaType);
        var resized = new SKBitmap(info);
        if (!source.ScalePixels(resized, SKSamplingOptions.Default))
        {
            resized.Dispose();
            throw new InvalidOperationException("resize_failed");
        }

        return resized;
    }

    public void Dispose() => _bitmap.Dispose();

    public SKBitmap ToBitmap() => _bitmap.Copy();
}
