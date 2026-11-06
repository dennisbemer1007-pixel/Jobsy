using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Fails if decorative hero/illustration layers can receive clicks (missing central guard CSS or local overrides).
/// </summary>
public sealed class DecorativeIllustrationLayersGateTests
{
    private static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly string CentralCssRelative =
        "Jobsy.Web/wwwroot/css/features/candidate-illustrated-decorative-layers.css";

    [Fact]
    public void Central_decorative_layers_stylesheet_exists_and_covers_global_patterns()
    {
        var path = Path.Combine(RepoRoot, CentralCssRelative);
        Assert.True(File.Exists(path));
        var css = File.ReadAllText(path);
        Assert.Contains("§decorative", css, StringComparison.Ordinal);
        Assert.Contains("[aria-hidden=\"true\"]", css, StringComparison.Ordinal);
        Assert.Contains("[class*=\"__art\"]", css, StringComparison.Ordinal);
        Assert.Contains(".cand-hero__copy", css, StringComparison.Ordinal);
        Assert.Contains(".cand-compact-hero__art", css, StringComparison.Ordinal);
        Assert.Contains(".test-block-stage__mobile-hero--band", css, StringComparison.Ordinal);
        Assert.Contains("isolation: isolate", css, StringComparison.Ordinal);
        Assert.Matches(@"(?s)\[aria-hidden=""true""\][^{]*\{[^}]*pointer-events:\s*none", css);
    }

    [Fact]
    public void Blazor_core_and_public_shell_load_central_decorative_layers_css()
    {
        var catalog = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Hosting/PageAssetCatalog.cs"));
        Assert.Contains("candidate-illustrated-decorative-layers.css", catalog, StringComparison.Ordinal);
        var app = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("candidate-illustrated-decorative-layers.css", app, StringComparison.Ordinal);
    }

    [Fact]
    public void Feature_css_does_not_enable_pointer_events_on_decorative_art_selectors()
    {
        var featuresDir = Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/css/features");
        var centralName = Path.GetFileName(CentralCssRelative);
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(featuresDir, "*.css"))
        {
            if (string.Equals(Path.GetFileName(file), centralName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var css = File.ReadAllText(file);
            foreach (Match block in Regex.Matches(css, @"(?s)([^{]+)\{([^}]*)\}"))
            {
                var selector = block.Groups[1].Value;
                var body = block.Groups[2].Value;
                if (!Regex.IsMatch(body, @"pointer-events\s*:\s*auto", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                if (!LooksLikeDecorativeArtSelector(selector))
                {
                    continue;
                }

                if (LooksLikeInteractiveContentSelector(selector))
                {
                    continue;
                }

                violations.Add($"{Path.GetFileName(file)}: {selector.Trim()}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Decorative art selectors must not set pointer-events:auto:\n" + string.Join("\n", violations.Take(20)));
    }

    [Fact]
    public void Illustrated_hero_razor_marks_art_with_aria_hidden_or_known_art_class()
    {
        var webRoot = Path.Combine(RepoRoot, "Jobsy.Web");
        var issues = new List<string>();
        foreach (var razor in Directory.EnumerateFiles(webRoot, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(razor);
            if (!text.Contains("__art", StringComparison.Ordinal)
                && !text.Contains("illustration-art", StringComparison.Ordinal)
                && !text.Contains("hero-art", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match m in Regex.Matches(text, @"<(?:div|figure|picture|span|img)[^>]*class=""[^""]*(?:__art|hero-art|illustration-art)[^""]*""[^>]*>", RegexOptions.IgnoreCase))
            {
                var tag = m.Value;
                if (tag.Contains("aria-hidden=\"true\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (tag.Contains("wa-guide__art", StringComparison.Ordinal)
                    || tag.Contains("ob-journey__art-block", StringComparison.Ordinal))
                {
                    continue;
                }

                var idx = m.Index;
                var windowStart = Math.Max(0, idx - 400);
                var window = text[windowStart..idx];
                if (window.Contains("aria-hidden=\"true\"", StringComparison.OrdinalIgnoreCase)
                    && window.LastIndexOf('<') > window.LastIndexOf('>'))
                {
                    continue;
                }

                var rel = Path.GetRelativePath(RepoRoot, razor);
                issues.Add($"{rel}: {tag[..Math.Min(tag.Length, 120)]}");
            }
        }

        Assert.True(
            issues.Count == 0,
            "Decorative art markup should carry aria-hidden=\"true\" (central CSS relies on it):\n"
            + string.Join("\n", issues.Take(25)));
    }

    private static bool LooksLikeDecorativeArtSelector(string selector)
    {
        return selector.Contains("__art", StringComparison.Ordinal)
               || selector.Contains("hero-art", StringComparison.Ordinal)
               || selector.Contains("illustration-art", StringComparison.Ordinal)
               || selector.Contains("ob-illustrated-step__hero-band", StringComparison.Ordinal)
               || selector.Contains("ob-illustrated-step__mobile-cover", StringComparison.Ordinal)
               || selector.Contains("cand-hero__media", StringComparison.Ordinal)
               || selector.Contains("cand-hero__mob-figures", StringComparison.Ordinal)
               || Regex.IsMatch(selector, @"\.cand-hero::?(before|after)", RegexOptions.IgnoreCase)
               || selector.Contains("kb-nearby-banner__art", StringComparison.Ordinal)
               || selector.Contains("ndm-hero__art", StringComparison.Ordinal)
               || selector.Contains(".ndm .band .art", StringComparison.Ordinal)
               || selector.Contains("test-block-stage__mobile-hero", StringComparison.Ordinal)
               || selector.Contains("cand-compact-hero__art", StringComparison.Ordinal);
    }

    private static bool LooksLikeInteractiveContentSelector(string selector)
    {
        return selector.Contains("__copy", StringComparison.Ordinal)
               || selector.Contains("__body", StringComparison.Ordinal)
               || selector.Contains("__form", StringComparison.Ordinal)
               || selector.Contains("__head", StringComparison.Ordinal)
               || selector.Contains("__mobile-cover-copy", StringComparison.Ordinal)
               || selector.Contains("hero-inner", StringComparison.Ordinal)
               || selector.Contains("filter-bar", StringComparison.Ordinal)
               || selector.Contains("step-pill", StringComparison.Ordinal);
    }
}
