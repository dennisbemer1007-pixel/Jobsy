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
        File.Copy(sourcePath, Path.Combine(dest, destName), overwrite: true);
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
            File.Copy(png, Path.Combine(dest, Path.GetFileName(png)), overwrite: true);
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

            File.Copy(src, Path.Combine(dest, $"{slug}-{tag}.png"), overwrite: true);
            File.Copy(src, Path.Combine(dest, tag == "mobile-390" ? "mobile-390@2x.png" : "desktop-1440.png"), overwrite: true);
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
