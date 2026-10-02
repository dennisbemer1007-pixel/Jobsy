using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Guards that public-theme.css stays scoped under .pub-theme and cannot leak into MainLayout.
/// </summary>
public class PublicThemeCssGuardTests
{
    private static readonly Regex HexOrRgb = new(
        @"#(?:[0-9a-fA-F]{3,8})\b|\brgba?\s*\(",
        RegexOptions.Compiled);

    [Fact]
    public void Public_theme_css_selectors_are_scoped_and_token_only()
    {
        var raw = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "public-theme.css"));
        var css = Regex.Replace(raw, @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotContain("!important", css, StringComparison.Ordinal);

        foreach (var block in SplitRules(css))
        {
            var selector = block.Selector.Trim();
            if (selector.Length == 0)
            {
                continue;
            }

            foreach (var part in selector.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                Assert.True(
                    part.Contains(".pub-theme", StringComparison.Ordinal),
                    $"Selector must include .pub-theme: {part}");
            }

            Assert.False(HexOrRgb.IsMatch(block.Body), $"No hex/rgb literals in public-theme.css near: {selector}");
        }
    }

    [Fact]
    public void Pub_classes_do_not_appear_in_MainLayout_pages()
    {
        var root = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages");
        foreach (var file in Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (text.Contains("@layout PublicLayout", StringComparison.Ordinal)
                || text.Contains("@layout TeaserLayout", StringComparison.Ordinal)
                || text.Contains("@layout ErrorLayout", StringComparison.Ordinal)
                || text.Contains("@layout Jobsy.Web.Components.Layout.WaPublicLayout", StringComparison.Ordinal)
                || text.Contains("@layout WaPublicLayout", StringComparison.Ordinal))
            {
                continue;
            }

            // Default layout is MainLayout for authenticated app pages.
            Assert.DoesNotContain("pub-", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pub-theme", text, StringComparison.OrdinalIgnoreCase);
        }

        var mainLayout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.DoesNotContain("pub-", mainLayout, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<(string Selector, string Body)> SplitRules(string css)
    {
        // Strip comments.
        css = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);
        var i = 0;
        while (i < css.Length)
        {
            var open = css.IndexOf('{', i);
            if (open < 0)
            {
                yield break;
            }

            var selector = css[i..open];
            var depth = 0;
            var j = open;
            for (; j < css.Length; j++)
            {
                if (css[j] == '{') depth++;
                else if (css[j] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        break;
                    }
                }
            }

            var body = css[(open + 1)..j];
            var trimmed = selector.TrimStart();
            // @keyframes stops (0%, from, to, …) are not theme selectors — skip the block.
            if (trimmed.StartsWith("@keyframes", StringComparison.Ordinal)
                || trimmed.StartsWith("@-webkit-keyframes", StringComparison.Ordinal))
            {
                // intentionally skip
            }
            else if (trimmed.StartsWith("@", StringComparison.Ordinal))
            {
                // @media / @supports: yield nested rules so they stay .pub-theme-scoped.
                foreach (var nested in SplitRules(body))
                {
                    yield return nested;
                }
            }
            else
            {
                yield return (selector, body);
            }

            i = j + 1;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
