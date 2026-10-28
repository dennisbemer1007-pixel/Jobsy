using System.Buffers.Binary;
using SkiaSharp;
using IOPath = System.IO.Path;

/// <summary>
/// Regenerates Lobsy PWA / favicon assets from the brand master PNG.
/// Usage (from repo root): dotnet run --project tools/generate-icons
/// </summary>
static class Program
{
    static readonly SKColor Coral = SKColor.Parse("#f54a1b");
    static readonly SKColor White = SKColors.White;

    const float BorderFraction = 0.07f;
    const float CornerRadiusFraction = 0.18f;
    const float AnyLogoHeightFraction = 0.78f;
    const float AnyLogoNudgeUpFraction = 0.01f;
    const float MaskableCircleDiameterFraction = 0.80f;
    const float MaskableLogoHeightFraction = 0.62f;

    static int Main(string[] _)
    {
        var repoRoot = FindRepoRoot();
        var brandLogo = IOPath.Combine(repoRoot, "Jobsy.Web", "wwwroot", "images", "brand", "lobsy.png");
        if (!File.Exists(brandLogo))
        {
            Console.Error.WriteLine($"Brand logo not found: {brandLogo}");
            return 1;
        }

        var wwwroot = IOPath.Combine(repoRoot, "Jobsy.Web", "wwwroot");
        var iconsDir = IOPath.Combine(wwwroot, "icons");
        Directory.CreateDirectory(iconsDir);

        using var master = SKBitmap.Decode(brandLogo) ?? throw new InvalidOperationException("decode brand");
        using var logoOpaque = TrimTransparent(master);

        Console.WriteLine($"Source: {brandLogo} ({master.Width}x{master.Height})");
        Console.WriteLine($"Trimmed logo: {logoOpaque.Width}x{logoOpaque.Height}");

        Save(ComposeAny(logoOpaque, 512), IOPath.Combine(iconsDir, "icon-512.png"));
        Save(ComposeAny(logoOpaque, 192), IOPath.Combine(iconsDir, "icon-192.png"));
        Save(ComposeAny(logoOpaque, 180), IOPath.Combine(iconsDir, "apple-touch-icon.png"));

        Save(ComposeMaskable(logoOpaque, 512), IOPath.Combine(iconsDir, "icon-maskable-512.png"));
        Save(ComposeMaskable(logoOpaque, 192), IOPath.Combine(iconsDir, "icon-maskable-192.png"));

        using var fav32 = ComposeAny(logoOpaque, 32, thickenLogo: true);
        using var fav16 = ComposeAny(logoOpaque, 16, thickenLogo: true);
        using var fav48 = ComposeAny(logoOpaque, 48, thickenLogo: true);
        Save(fav32.Copy(), IOPath.Combine(wwwroot, "favicon.png"));
        WriteIco(IOPath.Combine(wwwroot, "favicon.ico"), fav16, fav32, fav48);

        Console.WriteLine("Done.");
        return 0;
    }

    static SKBitmap ComposeAny(SKBitmap logo, int size, bool thickenLogo = false)
    {
        var canvas = new SKBitmap(size, size);
        using var sk = new SKCanvas(canvas);
        sk.Clear(Coral);

        var border = size * BorderFraction;
        var inner = size - (2f * border);
        var radius = size * CornerRadiusFraction;
        using var whitePaint = new SKPaint { Color = White, IsAntialias = true, Style = SKPaintStyle.Fill };
        sk.DrawRoundRect(new SKRect(border, border, border + inner, border + inner), radius, radius, whitePaint);

        var logoHeight = size * AnyLogoHeightFraction;
        var nudgeUp = size * AnyLogoNudgeUpFraction;
        DrawLogoCentered(sk, logo, logoHeight, verticalNudgeUp: nudgeUp, thicken: thickenLogo, canvasWidth: size);
        sk.Flush();
        return canvas;
    }

    static SKBitmap ComposeMaskable(SKBitmap logo, int size)
    {
        var canvas = new SKBitmap(size, size);
        using var sk = new SKCanvas(canvas);
        sk.Clear(Coral);

        var diameter = size * MaskableCircleDiameterFraction;
        var radius = diameter / 2f;
        var cx = size / 2f;
        var cy = size / 2f;
        using var whitePaint = new SKPaint { Color = White, IsAntialias = true, Style = SKPaintStyle.Fill };
        sk.DrawCircle(cx, cy, radius, whitePaint);

        var logoHeight = size * MaskableLogoHeightFraction;
        DrawLogoCentered(sk, logo, logoHeight, verticalNudgeUp: 0f, thicken: false, canvasWidth: size);
        sk.Flush();
        return canvas;
    }

    static void DrawLogoCentered(
        SKCanvas canvas,
        SKBitmap logo,
        float targetHeight,
        float verticalNudgeUp,
        bool thicken,
        int canvasWidth)
    {
        var scale = targetHeight / logo.Height;
        var w = Math.Max(1, (int)Math.Round(logo.Width * scale));
        var h = Math.Max(1, (int)Math.Round(logo.Height * scale));
        using var scaled = Resize(logo, w, h);
        if (thicken)
        {
            ThickenAlpha(scaled, radius: canvasWidth <= 16 ? 1 : 1);
        }

        var x = (canvasWidth - scaled.Width) / 2f;
        var y = (canvasWidth - scaled.Height) / 2f - verticalNudgeUp;
        canvas.DrawBitmap(scaled, x, y);
    }

    static void ThickenAlpha(SKBitmap image, int radius)
    {
        using var source = image.Copy();
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                SKColor best = default;
                var bestA = (byte)0;
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var sy = y + dy;
                    if ((uint)sy >= (uint)source.Height)
                    {
                        continue;
                    }

                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        var sx = x + dx;
                        if ((uint)sx >= (uint)source.Width)
                        {
                            continue;
                        }

                        var p = source.GetPixel(sx, sy);
                        if (p.Alpha > bestA)
                        {
                            bestA = p.Alpha;
                            best = p;
                        }
                    }
                }

                var dest = image.GetPixel(x, y);
                if (bestA > dest.Alpha)
                {
                    image.SetPixel(x, y, best);
                }
            }
        }
    }

    static SKBitmap TrimTransparent(SKBitmap source)
    {
        var minX = source.Width;
        var minY = source.Height;
        var maxX = -1;
        var maxY = -1;
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                if (source.GetPixel(x, y).Alpha < 8)
                {
                    continue;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        if (maxX < minX)
        {
            return source.Copy();
        }

        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        var cropped = new SKBitmap(w, h);
        using (var cropCanvas = new SKCanvas(cropped))
        {
            cropCanvas.DrawBitmap(source, -minX, -minY);
            cropCanvas.Flush();
        }

        return cropped;
    }

    static SKBitmap Resize(SKBitmap source, int width, int height)
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

    static void Save(SKBitmap image, string path)
    {
        using var encoded = SKImage.FromBitmap(image);
        using var data = encoded.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data!.ToArray());
        Console.WriteLine($"Wrote {path} ({image.Width}x{image.Height})");
        image.Dispose();
    }

    static void WriteIco(string path, params SKBitmap[] images)
    {
        var pngs = new List<byte[]>(images.Length);
        foreach (var img in images)
        {
            using var encoded = SKImage.FromBitmap(img);
            using var data = encoded.Encode(SKEncodedImageFormat.Png, 100);
            pngs.Add(data!.ToArray());
        }

        using var fs = File.Create(path);
        Span<byte> header = stackalloc byte[6];
        BinaryPrimitives.WriteUInt16LittleEndian(header, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(header[2..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[4..], (ushort)images.Length);
        fs.Write(header);

        var offset = 6 + (16 * images.Length);
        Span<byte> entry = stackalloc byte[16];
        for (var i = 0; i < images.Length; i++)
        {
            var img = images[i];
            entry.Clear();
            entry[0] = (byte)(img.Width >= 256 ? 0 : img.Width);
            entry[1] = (byte)(img.Height >= 256 ? 0 : img.Height);
            BinaryPrimitives.WriteUInt16LittleEndian(entry[4..], 1);
            BinaryPrimitives.WriteUInt16LittleEndian(entry[6..], 32);
            BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)pngs[i].Length);
            BinaryPrimitives.WriteUInt32LittleEndian(entry[12..], (uint)offset);
            fs.Write(entry);
            offset += pngs[i].Length;
        }

        foreach (var png in pngs)
        {
            fs.Write(png);
        }

        Console.WriteLine($"Wrote {path} ({images.Length} sizes)");
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(IOPath.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(IOPath.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Jobsy.sln (repo root).");
    }
}
