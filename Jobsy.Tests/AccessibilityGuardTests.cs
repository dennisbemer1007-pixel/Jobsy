namespace Jobsy.Tests;

/// <summary>
/// Source-level guards for Lighthouse a11y: valid ARIA token values and WCAG AA contrast
/// on the public cookie banner primary button.
/// </summary>
public class AccessibilityGuardTests
{
    [Fact]
    public void Discovery_filter_toggles_use_valid_aria_tokens_and_keep_the_desktop_panel_in_the_dom()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));

        Assert.Contains("aria-expanded=\"@(_filtersOpen ? \"true\" : \"false\")\"", discovery);
        Assert.DoesNotContain("aria-expanded=\"@_filtersOpen\"", discovery);
        Assert.DoesNotContain("aria-expanded=\"@_branchMenuOpen\"", discovery);
        Assert.DoesNotContain("aria-expanded=\"@_categoryMenuOpen\"", discovery);
        Assert.DoesNotContain("aria-pressed=\"@_myVacanciesOnly\"", discovery);
        Assert.DoesNotContain("aria-busy=\"@_locating\"", discovery);

        Assert.Contains("aria-controls=\"discovery-filters-desktop\"", discovery);
        Assert.Contains("aria-controls=\"@(_filtersOpen ? \"discovery-filters\" : null)\"", discovery);

        var panelAt = discovery.IndexOf("id=\"discovery-filters-desktop\"", StringComparison.Ordinal);
        Assert.True(panelAt > 0);
        var panelHead = discovery[Math.Max(0, panelAt - 180)..Math.Min(discovery.Length, panelAt + 220)];
        Assert.DoesNotContain("@if (_filtersOpen)", panelHead);
        Assert.Contains("hidden=\"@(!_filtersOpen)\"", panelHead);
        Assert.Contains("aria-hidden=\"@(!_filtersOpen ? \"true\" : \"false\")\"", panelHead);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains(".filter-bar--desktop[hidden]", css);
        Assert.Contains("display: none !important", css);
    }

    [Fact]
    public void Cookie_accept_analytics_button_meets_wcag_aa_contrast()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        var marker = ".cookie-consent__actions .btn-compact--primary {";
        var start = css.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start > 0);
        var end = css.IndexOf('}', start);
        Assert.True(end > start);
        var block = css[start..end];
        Assert.Contains("--coral-deep", block);
        Assert.Contains("#fff", block);
        Assert.DoesNotContain("#f54a1b", block);

        Assert.True(
            ContrastRatio(0xFFFFFF, 0xD93A12) >= 4.5,
            "White on --coral-deep (#d93a12) must meet WCAG AA for the compact cookie CTA.");
    }

    [Fact]
    public void Public_chrome_does_not_bind_aria_expanded_to_a_raw_bool()
    {
        var files = new[]
        {
            Path.Combine("Jobsy.Web", "Components", "Layout", "LanguageSelector.razor"),
            Path.Combine("Jobsy.Web", "Components", "Layout", "NotificationBell.razor"),
            Path.Combine("Jobsy.Web", "Components", "Layout", "PageHelp.razor"),
            Path.Combine("Jobsy.Web", "Components", "Feedback", "FeedbackWidget.razor")
        };

        var root = FindRepoRoot();
        foreach (var relative in files)
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.DoesNotContain("aria-expanded=\"@_", text);
            Assert.Contains("? \"true\" : \"false\"", text);
        }
    }

    [Fact]
    public void Featured_carousel_keeps_an_accessible_name_without_repeating_uitgelicht()
    {
        var carousel = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "HighlightVacancyCarousel.razor"));
        Assert.Contains("aria-label=\"@Culture[\"Discovery.Featured\"]\"", carousel);
        Assert.DoesNotContain("class=\"highlight-carousel__title\"", carousel);
        Assert.DoesNotContain("highlight-carousel__badge", carousel);
        Assert.DoesNotContain("@Culture[\"Discovery.Featured\"]</p>", carousel);
        Assert.DoesNotContain("@Culture[\"Discovery.Featured\"]</span>", carousel);
    }

    private static double ContrastRatio(int fgRgb, int bgRgb)
    {
        var l1 = RelativeLuminance(fgRgb);
        var l2 = RelativeLuminance(bgRgb);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(int rgb)
    {
        static double Channel(int value)
        {
            var srgb = value / 255.0;
            return srgb <= 0.04045
                ? srgb / 12.92
                : Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }

        var r = Channel((rgb >> 16) & 0xFF);
        var g = Channel((rgb >> 8) & 0xFF);
        var b = Channel(rgb & 0xFF);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
