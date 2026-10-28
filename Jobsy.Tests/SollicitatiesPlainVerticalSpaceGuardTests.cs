namespace Jobsy.Tests;

/// <summary>Dennis: geen lege stroken op plain Sollicitaties/Bewaard (geen hero-reserve, geen min-height vullers).</summary>
public sealed class SollicitatiesPlainVerticalSpaceGuardTests
{
    [Fact]
    public void Plain_css_has_no_min_height_fill_on_page_wrap()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/candidate-plain-app.css"));
        Assert.DoesNotContain("min-height: 100%", css, StringComparison.Ordinal);
        Assert.Contains("[data-plain-app=\"sollicitaties\"]", css, StringComparison.Ordinal);
        Assert.Contains("padding-block-start: 0", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_ssr_skips_kreeft_strip_and_uses_plain_saved_intro()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var apps = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Applications/ApplicationsSsrShells.cs"));
        var saved = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Saved/SavedSsrShells.cs"));
        Assert.Contains("if (!plainShell)", apps, StringComparison.Ordinal);
        Assert.Contains("AppendHeroStrip", apps, StringComparison.Ordinal);
        Assert.Contains("sollicitaties-intro", saved, StringComparison.Ordinal);
        Assert.Contains("saved-empty-plain", saved, StringComparison.Ordinal);
        Assert.Contains("if (plainShell)", saved, StringComparison.Ordinal);
    }
}
