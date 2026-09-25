using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IOPath = System.IO.Path;

/// <summary>
/// Regenerates Lobsy PWA / favicon assets from the brand master PNG.
/// Usage (from repo root): dotnet run --project tools/generate-icons
/// </summary>
static class Program
{
    // Brand coral (--coral)
    static readonly Color Coral = Color.ParseHex("#f54a1b");
    static readonly Color White = Color.ParseHex("#ffffff");

    // Layout ratios (fraction of canvas size)
    const float BorderFraction = 0.07f;
    const float CornerRadiusFraction = 0.18f;
    const float AnyLogoHeightFraction = 0.78f;
    const float AnyLogoNudgeUpFraction = 0.01f;
    const float MaskableCircleDiameterFraction = 0.80f;
    const float MaskableLogoHeightFraction = 0.62f;

    static int Main(string[] args)
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

        using var master = Image.Load<Rgba32>(brandLogo);
        using var logoOpaque = TrimTransparent(master);

        Console.WriteLine($"Source: {brandLogo} ({master.Width}x{master.Height})");
        Console.WriteLine($"Trimmed logo: {logoOpaque.Width}x{logoOpaque.Height}");

        // purpose=any / apple-touch (square coral frame + white rounded square)
        Save(ComposeAny(logoOpaque, 512), IOPath.Combine(iconsDir, "icon-512.png"));
        Save(ComposeAny(logoOpaque, 192), IOPath.Combine(iconsDir, "icon-192.png"));
        Save(ComposeAny(logoOpaque, 180), IOPath.Combine(iconsDir, "apple-touch-icon.png"));

        // purpose=maskable (full coral + white circle, opaque)
        Save(ComposeMaskable(logoOpaque, 512), IOPath.Combine(iconsDir, "icon-maskable-512.png"));
        Save(ComposeMaskable(logoOpaque, 192), IOPath.Combine(iconsDir, "icon-maskable-192.png"));

        // Favicons — slightly thicker antennae / fine detail
        using var fav32 = ComposeAny(logoOpaque, 32, thickenLogo: true);
        using var fav16 = ComposeAny(logoOpaque, 16, thickenLogo: true);
        using var fav48 = ComposeAny(logoOpaque, 48, thickenLogo: true);
        Save(fav32.Clone(), IOPath.Combine(wwwroot, "favicon.png"));
        WriteIco(IOPath.Combine(wwwroot, "favicon.ico"), fav16, fav32, fav48);

        Console.WriteLine("Done.");
        return 0;
    }

    static Image<Rgba32> ComposeAny(Image<Rgba32> logo, int size, bool thickenLogo = false)
    {
        var canvas = new Image<Rgba32>(size, size);
        canvas.Mutate(ctx => ctx.Fill(Coral));

        float border = size * BorderFraction;
        float inner = size - 2f * border;
        float radius = size * CornerRadiusFraction;
        FillRoundedRectangle(canvas, border, border, inner, inner, radius, White);

        float logoHeight = size * AnyLogoHeightFraction;
        float nudgeUp = size * AnyLogoNudgeUpFraction;
        DrawLogoCentered(canvas, logo, logoHeight, verticalNudgeUp: nudgeUp, thicken: thickenLogo);
        return canvas;
    }

    static Image<Rgba32> ComposeMaskable(Image<Rgba32> logo, int size)
    {
        var canvas = new Image<Rgba32>(size, size);
        // Opaque coral — no transparency (Android circular crop)
        canvas.Mutate(ctx => ctx.Fill(Coral));

        float diameter = size * MaskableCircleDiameterFraction;
        float radius = diameter / 2f;
        float cx = size / 2f;
        float cy = size / 2f;
        var circle = new EllipsePolygon(cx, cy, radius);
        canvas.Mutate(ctx => ctx.Fill(White, circle));

        float logoHeight = size * MaskableLogoHeightFraction;
        DrawLogoCentered(canvas, logo, logoHeight, verticalNudgeUp: 0f, thicken: false);
        return canvas;
    }

    static void DrawLogoCentered(
        Image<Rgba32> canvas,
        Image<Rgba32> logo,
        float targetHeight,
        float verticalNudgeUp,
        bool thicken)
    {
        float scale = targetHeight / logo.Height;
        int w = Math.Max(1, (int)Math.Round(logo.Width * scale));
        int h = Math.Max(1, (int)Math.Round(logo.Height * scale));

        using var scaled = logo.Clone(ctx =>
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(w, h),
                Mode = ResizeMode.Stretch,
                Sampler = KnownResamplers.Lanczos3
            }));

        if (thicken)
        {
            ThickenAlpha(scaled, radius: canvas.Width <= 16 ? 1 : 1);
        }

        int x = (canvas.Width - scaled.Width) / 2;
        int y = (int)Math.Round((canvas.Height - scaled.Height) / 2f - verticalNudgeUp);
        canvas.Mutate(ctx => ctx.DrawImage(scaled, new Point(x, y), 1f));
    }

    /// <summary>
    /// Morphological dilation on alpha so thin antennae survive 16–48px favicons.
    /// </summary>
    static void ThickenAlpha(Image<Rgba32> image, int radius)
    {
        using var source = image.Clone();
        image.ProcessPixelRows(source, (destAccessor, srcAccessor) =>
        {
            for (int y = 0; y < destAccessor.Height; y++)
            {
                var destRow = destAccessor.GetRowSpan(y);
                for (int x = 0; x < destRow.Length; x++)
                {
                    Rgba32 best = default;
                    byte bestA = 0;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int sy = y + dy;
                        if ((uint)sy >= (uint)srcAccessor.Height) continue;
                        var srcRow = srcAccessor.GetRowSpan(sy);
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int sx = x + dx;
                            if ((uint)sx >= (uint)srcRow.Length) continue;
                            var p = srcRow[sx];
                            if (p.A > bestA)
                            {
                                bestA = p.A;
                                best = p;
                            }
                        }
                    }

                    if (bestA > destRow[x].A)
                        destRow[x] = best;
                }
            }
        });
    }

    /// <summary>Filled rounded rect as union of two rects + four corner circles.</summary>
    static void FillRoundedRectangle(Image<Rgba32> canvas, float x, float y, float width, float height, float radius, Color color)
    {
        radius = Math.Clamp(radius, 0f, Math.Min(width, height) / 2f);
        IPath[] parts =
        [
            new RectangularPolygon(x + radius, y, width - 2 * radius, height),
            new RectangularPolygon(x, y + radius, width, height - 2 * radius),
            new EllipsePolygon(x + radius, y + radius, radius),
            new EllipsePolygon(x + width - radius, y + radius, radius),
            new EllipsePolygon(x + radius, y + height - radius, radius),
            new EllipsePolygon(x + width - radius, y + height - radius, radius),
        ];
        canvas.Mutate(ctx =>
        {
            foreach (var part in parts)
                ctx.Fill(color, part);
        });
    }

    static Image<Rgba32> TrimTransparent(Image<Rgba32> source)
    {
        int minX = source.Width, minY = source.Height, maxX = -1, maxY = -1;
        source.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].A < 8) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        });

        if (maxX < minX)
            return source.Clone();

        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        return source.Clone(ctx => ctx.Crop(new Rectangle(minX, minY, w, h)));
    }

    static void Save(Image<Rgba32> image, string path)
    {
        var encoder = new PngEncoder
        {
            ColorType = PngColorType.RgbWithAlpha,
            CompressionLevel = PngCompressionLevel.BestCompression
        };
        image.Save(path, encoder);
        Console.WriteLine($"Wrote {path} ({image.Width}x{image.Height})");
        image.Dispose();
    }

    /// <summary>Writes a multi-size ICO with embedded PNG frames (16 / 32 / 48).</summary>
    static void WriteIco(string path, params Image<Rgba32>[] images)
    {
        var pngs = new List<byte[]>(images.Length);
        var encoder = new PngEncoder
        {
            ColorType = PngColorType.RgbWithAlpha,
            CompressionLevel = PngCompressionLevel.BestCompression
        };
        foreach (var img in images)
        {
            using var ms = new MemoryStream();
            img.Save(ms, encoder);
            pngs.Add(ms.ToArray());
        }

        using var fs = File.Create(path);
        // ICONDIR
        Span<byte> header = stackalloc byte[6];
        BinaryPrimitives.WriteUInt16LittleEndian(header, 0); // reserved
        BinaryPrimitives.WriteUInt16LittleEndian(header[2..], 1); // type = icon
        BinaryPrimitives.WriteUInt16LittleEndian(header[4..], (ushort)images.Length);
        fs.Write(header);

        int offset = 6 + 16 * images.Length;
        Span<byte> entry = stackalloc byte[16];
        for (int i = 0; i < images.Length; i++)
        {
            var img = images[i];
            entry.Clear();
            entry[0] = (byte)(img.Width >= 256 ? 0 : img.Width);
            entry[1] = (byte)(img.Height >= 256 ? 0 : img.Height);
            entry[2] = 0; // colors
            entry[3] = 0; // reserved
            BinaryPrimitives.WriteUInt16LittleEndian(entry[4..], 1); // planes
            BinaryPrimitives.WriteUInt16LittleEndian(entry[6..], 32); // bit count
            BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)pngs[i].Length);
            BinaryPrimitives.WriteUInt32LittleEndian(entry[12..], (uint)offset);
            fs.Write(entry);
            offset += pngs[i].Length;
        }

        foreach (var png in pngs)
            fs.Write(png);

        Console.WriteLine($"Wrote {path} ({images.Length} sizes)");
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(IOPath.Combine(dir.FullName, "Jobsy.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        // When running via `dotnet run --project`, BaseDirectory is bin/...; walk from cwd too.
        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(IOPath.Combine(dir.FullName, "Jobsy.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Jobsy.sln (repo root).");
    }
}
