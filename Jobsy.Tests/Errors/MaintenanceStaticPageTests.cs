using System.Text.RegularExpressions;

namespace Jobsy.Tests.Errors;

/// <summary>
/// errors 05 §05.6. The static edge pages must survive a total outage: one self-contained file,
/// no scripts, all five languages, and the Cloudflare variant carries the required token.
/// </summary>
public class MaintenanceStaticPageTests
{
    private const int MaxBytes = 100 * 1024;

    private static readonly string[] ExpectedLanguages = ["nl", "en", "pl", "ro", "ar"];

    [Theory]
    [InlineData("index.html")]
    [InlineData("cloudflare-500.html")]
    public void Page_is_small_enough_to_inline_at_the_edge(string file)
    {
        var info = new FileInfo(PathTo(file));
        Assert.True(info.Exists, $"ops/maintenance/{file} is missing.");
        Assert.True(info.Length <= MaxBytes, $"{file} is {info.Length} bytes, limit is {MaxBytes}.");
    }

    [Theory]
    [InlineData("index.html")]
    [InlineData("cloudflare-500.html")]
    public void Page_has_no_scripts_and_no_external_resources(string file)
    {
        var html = File.ReadAllText(PathTo(file));

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<link", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", html, StringComparison.OrdinalIgnoreCase);
        // The only https:// allowed is inside the explanatory comment, never in markup.
        foreach (var line in html.Split('\n'))
        {
            if (line.Contains("https://", StringComparison.OrdinalIgnoreCase))
            {
                Assert.DoesNotContain("src=", line, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("href=\"https", line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("index.html")]
    [InlineData("cloudflare-500.html")]
    public void Page_is_noindex_and_carries_all_five_languages(string file)
    {
        var html = File.ReadAllText(PathTo(file));

        Assert.Contains("name=\"robots\"", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.Ordinal);

        foreach (var lang in ExpectedLanguages)
        {
            Assert.Contains($"lang=\"{lang}\"", html, StringComparison.Ordinal);
        }

        // Arabic is right-to-left (E5).
        Assert.Matches(new Regex("lang=\"ar\"[^>]*dir=\"rtl\"", RegexOptions.None, TimeSpan.FromSeconds(2)), html);
    }

    [Theory]
    [InlineData("index.html")]
    [InlineData("cloudflare-500.html")]
    public void Page_shows_the_calm_dutch_copy_and_the_support_address(string file)
    {
        var html = File.ReadAllText(PathTo(file));

        Assert.Contains("We zijn even aan het klussen", html, StringComparison.Ordinal);
        Assert.Contains("Lobsy is zo terug.", html, StringComparison.Ordinal);
        Assert.Contains("support@lobsy.nl", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Cloudflare_variant_carries_the_required_token_exactly_once()
    {
        var html = File.ReadAllText(PathTo("cloudflare-500.html"));

        Assert.Single(
            Regex.Matches(html, "::CLOUDFLARE_ERROR_500S_BOX::", RegexOptions.None, TimeSpan.FromSeconds(2)));

        // Cloudflare drops its diagnostics box when a referrer meta tag is present.
        Assert.DoesNotContain("name=\"referrer\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plain_variant_has_no_cloudflare_token()
        => Assert.DoesNotContain(
            "::CLOUDFLARE_ERROR_500S_BOX::",
            File.ReadAllText(PathTo("index.html")),
            StringComparison.Ordinal);

    [Fact]
    public void Runbook_documents_the_plan_check_with_dates_and_urls()
    {
        var doc = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "onderhoud.md"));

        Assert.Contains("2026-10-01", doc, StringComparison.Ordinal);
        Assert.Contains("developers.cloudflare.com/rules/custom-errors", doc, StringComparison.Ordinal);
        Assert.Contains("render.com/docs/maintenance-mode", doc, StringComparison.Ordinal);
        Assert.Contains("Wat Dennis moet beslissen", doc, StringComparison.Ordinal);
    }

    private static string PathTo(string file)
        => Path.Combine(RepoRoot(), "ops", "maintenance", file);

    private static string RepoRoot()
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
