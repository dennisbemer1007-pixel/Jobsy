using Jobsy.Tests.Testing;
using SkiaSharp;

namespace Jobsy.Tests.E2eFlow;

/// <summary>Copies screenshots/PDFs into /cursor/stores/self/fixes/{item}/ as captures land.</summary>
internal static class FixEvidencePublisher
{
    public const string FixesRoot = "/cursor/stores/self/fixes";
    public static string ScreensRoot => E2eFlowRunner.Screens;
    public static string PdfRoot => E2eFlowRunner.PdfDir;
    public static string EvidenceRoot => Path.Combine(E2eFlowRunner.Root, "evidence");

    public static void PublishItem(string itemFolder, params string[] slugPrefixes)
    {
        var dest = Path.Combine(FixesRoot, itemFolder);
        Directory.CreateDirectory(dest);
        foreach (var slug in slugPrefixes)
        {
            CopySlugPair(dest, slug);
        }

        TouchStatus(itemFolder);
    }

    public static void CopyFileToItem(string itemFolder, string sourcePath, string destName)
    {
        if (!File.Exists(sourcePath))
        {
            return;
        }

        var dest = Path.Combine(FixesRoot, itemFolder);
        Directory.CreateDirectory(dest);
        SafeCopyToStore(sourcePath, Path.Combine(dest, destName));
        TouchStatus(itemFolder);
    }

    public static void CopyPdfPagesToItem(string itemFolder, string pdfBaseName)
    {
        var dest = Path.Combine(FixesRoot, itemFolder);
        Directory.CreateDirectory(dest);
        if (!Directory.Exists(PdfRoot))
        {
            return;
        }

        foreach (var png in Directory.GetFiles(PdfRoot, $"{pdfBaseName}*.png"))
        {
            SafeCopyToStore(png, Path.Combine(dest, Path.GetFileName(png)));
        }

        TouchStatus(itemFolder);
    }

    private static void CopySlugPair(string dest, string slug)
    {
        foreach (var tag in new[] { "desktop-1440", "mobile-390" })
        {
            var src = Path.Combine(ScreensRoot, $"{slug}-{tag}.png");
            if (!File.Exists(src))
            {
                continue;
            }

            SafeCopyToStore(src, Path.Combine(dest, $"{slug}-{tag}.png"));
            SafeCopyToStore(
                src,
                Path.Combine(dest, tag == "mobile-390" ? "mobile-390@2x.png" : "desktop-1440.png"));
        }
    }

    /// <summary>Agent store FUSE has small per-file limits; fall back to JPEG when raw copy fails.</summary>
    private static void SafeCopyToStore(string sourcePath, string destPath)
    {
        try
        {
            File.Copy(sourcePath, destPath, overwrite: true);
            return;
        }
        catch (Exception ex) when (ex is IOException or ArgumentOutOfRangeException)
        {
            // fall through to compress
        }

        try
        {
            using var stream = File.OpenRead(sourcePath);
            using var decoded = SKBitmap.Decode(stream);
            if (decoded is null)
            {
                return;
            }

            using var resized = SkiaTestImage.ResizeMax(decoded, 1440, 9000);
            var jpegDest = Path.ChangeExtension(destPath, ".jpg");
            File.WriteAllBytes(jpegDest, SkiaTestImage.EncodeJpeg(resized, 82));
        }
        catch
        {
            // best-effort evidence
        }
    }

    private static void TouchStatus(string itemFolder)
    {
        var statusPath = Path.Combine(FixesRoot, "status.md");
        var line = $"- **{itemFolder}**: updated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n";
        if (File.Exists(statusPath))
        {
            var text = File.ReadAllText(statusPath);
            if (!text.Contains(itemFolder, StringComparison.Ordinal))
            {
                File.AppendAllText(statusPath, line);
            }
        }
    }
}
