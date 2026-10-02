using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Design-system guards for the Carrière stack (§0): tokens only, no inline styles other than
/// CSS custom properties, no native confirm and no raw exception text on candidate surfaces.
/// </summary>
public class CareerDesignGuardTests
{
    private static readonly Regex HexColour = new(@"#[0-9a-fA-F]{3,8}\b", RegexOptions.Compiled);
    private static readonly Regex InlineStyle = new(@"style\s*=\s*""", RegexOptions.Compiled);
    private static readonly Regex CustomPropStyle = new(@"style\s*=\s*""--", RegexOptions.Compiled);

    [Fact]
    public void Career_surfaces_use_tokens_only()
    {
        foreach (var file in CareerFiles())
        {
            var text = File.ReadAllText(file);
            Assert.False(
                HexColour.IsMatch(text),
                $"{Rel(file)} contains a hex colour; use design tokens (§0).");
            Assert.DoesNotContain("rgb(", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Career_surfaces_have_no_inline_styles_except_custom_properties()
    {
        foreach (var file in CareerFiles())
        {
            var text = File.ReadAllText(file);
            foreach (Match match in InlineStyle.Matches(text))
            {
                var slice = text.Substring(match.Index, Math.Min(12, text.Length - match.Index));
                Assert.True(
                    CustomPropStyle.IsMatch(slice),
                    $"{Rel(file)} has an inline style that is not a CSS custom property: {slice}");
            }
        }
    }

    [Fact]
    public void Career_code_never_uses_a_native_confirm()
    {
        foreach (var file in CareerFiles().Concat([PagePath()]))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("window.confirm", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"confirm\"", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Career_surfaces_never_render_exception_messages()
    {
        foreach (var file in CareerFiles().Concat([PagePath()]))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Career_page_has_no_percentages_or_removed_copy()
    {
        var text = File.ReadAllText(PagePath());
        Assert.DoesNotContain("HorizonArt", text, StringComparison.Ordinal);
        Assert.DoesNotContain("career-dash", text, StringComparison.Ordinal);
        Assert.DoesNotContain("horizon-", text, StringComparison.Ordinal);
        Assert.DoesNotContain("datalist", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MatchPercent", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Career_copy_avoids_jargon_and_old_metaphors()
    {
        var strings = File.ReadAllText(Path.Combine(
            Repo(), "Jobsy.Web", "Localization", "UiStringsCareer.cs"));

        foreach (var banned in new[] { "stip op de horizon", "Stip op de horizon", "DNA", "gap-analyse", "skills gap", "✓" })
        {
            Assert.DoesNotContain(banned, strings, StringComparison.Ordinal);
        }
    }

    private static string PagePath()
        => Path.Combine(Repo(), "Jobsy.Web", "Components", "Pages", "Candidate", "CareerDashboard.razor");

    private static IEnumerable<string> CareerFiles()
    {
        var root = Path.Combine(Repo(), "Jobsy.Web");
        var dirs = new[]
        {
            Path.Combine(root, "Components", "Candidate", "Career"),
            Path.Combine(root, "Components", "Candidate", "Journey")
        };

        foreach (var dir in dirs)
        {
            Assert.True(Directory.Exists(dir), $"Missing {dir}");
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".razor", StringComparison.Ordinal)
                            || f.EndsWith(".cs", StringComparison.Ordinal)))
            {
                yield return file;
            }
        }

        yield return Path.Combine(root, "wwwroot", "css", "features", "carriere.css");
    }

    private static string Rel(string path)
        => Path.GetRelativePath(Repo(), path);

    private static string Repo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }
}
