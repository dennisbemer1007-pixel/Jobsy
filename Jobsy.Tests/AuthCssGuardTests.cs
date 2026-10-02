using System.Text.RegularExpressions;

namespace Jobsy.Tests;

public sealed class AuthCssGuardTests
{
    [Fact]
    public void Auth_css_uses_logical_properties_and_tokens_only()
    {
        var root = FindRepoRoot();
        var cssPath = Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "auth.css");
        Assert.True(File.Exists(cssPath), cssPath);
        var css = File.ReadAllText(cssPath);
        var physical = new List<string>();
        foreach (var (i, line) in css.Split('\n').Select((l, idx) => (idx + 1, l)))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("/*", StringComparison.Ordinal) || trimmed.StartsWith("*", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.Contains("[dir=\"ltr\"]", StringComparison.Ordinal)
                || trimmed.Contains("[dir='ltr']", StringComparison.Ordinal))
            {
                continue;
            }

            if (Regex.IsMatch(line, @"(^|[^-])(left|right)\s*:")
                || Regex.IsMatch(line, @"(margin|padding|border)-(left|right)\s*:")
                || Regex.IsMatch(line, @"text-align:\s*(left|right)")
                || Regex.IsMatch(line, @"float:\s*(left|right)"))
            {
                physical.Add($"{i}: {trimmed}");
            }
        }

        Assert.True(physical.Count == 0, "Physical left/right in auth.css:\n" + string.Join("\n", physical));
        Assert.False(Regex.IsMatch(css, @"#[0-9a-fA-F]{3,8}\b"), "auth.css must not use hex color literals");
        Assert.False(Regex.IsMatch(css, @"rgba?\("), "auth.css must not use rgb/rgba literals");
    }

    [Fact]
    public void Auth_razor_pages_have_no_inline_style_attribute()
    {
        var root = FindRepoRoot();
        var paths = Directory.EnumerateFiles(
                Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Account"),
                "*.razor",
                SearchOption.TopDirectoryOnly)
            .Append(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Login.razor"));

            foreach (var file in paths)
        {
            if (!File.Exists(file) || Path.GetFileName(file).Equals("MailSettings.razor", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotMatch(@"\sstyle\s*=", text);
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
