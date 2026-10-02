using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Guards that public-pages.css stays scoped under <c>.pub-theme</c>, uses logical properties
/// (RTL) and cannot leak into MainLayout pages.
/// </summary>
public class PublicPagesCssGuardTests
{
    private static readonly Regex PhysicalProperty = new(
        @"(?<![\w-])(?:left|right)\s*:",
        RegexOptions.Compiled);

    [Fact]
    public void Every_selector_is_scoped_under_the_public_theme()
    {
        foreach (var block in SplitRules(ReadCss()))
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
        }
    }

    [Fact]
    public void No_important_and_no_physical_left_or_right_properties()
    {
        var css = ReadCss();
        Assert.DoesNotContain("!important", css, StringComparison.Ordinal);

        var hit = PhysicalProperty.Match(css);
        Assert.False(
            hit.Success,
            $"Use logical properties (inset-inline-*, margin-inline-*) instead of '{hit.Value}'.");
    }

    [Fact]
    public void Print_stylesheet_hides_the_chrome_and_opens_the_document()
    {
        var css = ReadCss();
        Assert.Contains("@media print", css, StringComparison.Ordinal);

        var print = css[css.IndexOf("@media print", StringComparison.Ordinal)..];
        foreach (var hidden in new[] { ".pub-header", ".pub-footer", ".pp-toc", ".pp-toc-mobile" })
        {
            Assert.Contains(hidden, print, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Pp_classes_do_not_appear_in_MainLayout_pages()
    {
        var pages = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages");
        foreach (var file in Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (text.Contains("@layout PublicLayout", StringComparison.Ordinal)
                || text.Contains("@layout TeaserLayout", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.DoesNotContain("\"pp-", text, StringComparison.Ordinal);
            Assert.DoesNotContain("class=\"pp", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Legal_components_have_no_inline_styles()
    {
        var legal = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Legal");
        foreach (var file in Directory.EnumerateFiles(legal, "*.razor", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("style=", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    private static string ReadCss()
    {
        var path = Path.Combine(
            FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "public-pages.css");
        Assert.True(File.Exists(path), "Missing wwwroot/css/features/public-pages.css");
        return Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static IEnumerable<(string Selector, string Body)> SplitRules(string css)
    {
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
                if (css[j] == '{')
                {
                    depth++;
                }
                else if (css[j] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        break;
                    }
                }
            }

            var body = css[(open + 1)..Math.Min(j, css.Length)];
            var trimmed = selector.TrimStart();
            if (trimmed.StartsWith("@keyframes", StringComparison.Ordinal))
            {
                // Keyframe stops are not theme selectors.
            }
            else if (trimmed.StartsWith('@'))
            {
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
