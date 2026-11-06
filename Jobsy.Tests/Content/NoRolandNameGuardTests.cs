using System.Text.RegularExpressions;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests.Content;

public sealed class NoRolandNameGuardTests
{
    private static readonly Regex ForbiddenDemoName = new(
        "\\b" + string.Concat("rol", "and") + "\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] ScanRoots =
    [
        "Jobsy.Api",
        "Jobsy.Core",
        "Jobsy.Infrastructure",
        "Jobsy.Tests",
        "Jobsy.Web",
        "HonestAdviceGen",
        "tools",
    ];

    private static readonly string[] SkipPathSegments =
    [
        $"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}mockups{Path.DirectorySeparatorChar}",
        $"{Path.AltDirectorySeparatorChar}docs{Path.AltDirectorySeparatorChar}mockups{Path.AltDirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
    ];

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".razor", ".js", ".json", ".md", ".txt", ".html", ".css", ".ts", ".tsx", ".mjs", ".csv", ".xml", ".props", ".targets", ".yml", ".yaml"
    };

    [Fact]
    public void Repository_has_no_roland_outside_docs_mockups()
    {
        var root = RepoRoot.Find();
        var hits = new List<string>();

        foreach (var scanRoot in ScanRoots)
        {
            var dir = Path.Combine(root, scanRoot);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                if (ShouldSkipPath(file))
                {
                    continue;
                }

                var ext = Path.GetExtension(file);
                if (!TextExtensions.Contains(ext))
                {
                    continue;
                }

                if (!LooksLikeText(file))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                if (string.Equals(Path.GetFileName(file), "NoRolandNameGuardTests.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!ForbiddenDemoName.IsMatch(text))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(root, file);
                hits.Add(relative.Replace('\\', '/'));
            }
        }

        Assert.True(
            hits.Count == 0,
            "The forbidden demo name must not appear outside docs/mockups (#878). Found in:\n"
            + string.Join('\n', hits.OrderBy(h => h, StringComparer.Ordinal)));
    }

    private static bool ShouldSkipPath(string fullPath)
    {
        foreach (var segment in SkipPathSegments)
        {
            if (fullPath.Contains(segment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeText(string path)
    {
        try
        {
            Span<byte> head = stackalloc byte[8192];
            using var stream = File.OpenRead(path);
            var read = stream.Read(head);
            if (read == 0)
            {
                return true;
            }

            for (var i = 0; i < read; i++)
            {
                if (head[i] == 0)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
