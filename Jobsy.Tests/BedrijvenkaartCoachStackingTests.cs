namespace Jobsy.Tests;

/// <summary>Regression for #1288: bedrijvenkaart controls must stack above the fixed coach pill.</summary>
public sealed class BedrijvenkaartCoachStackingTests
{
    [Fact]
    public void Bedrijvenkaart_css_raises_filters_and_tabs_above_coach()
    {
        var root = TestRepo.FindRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/bedrijvenkaart.css"));
        Assert.Contains(".app-shell:has(.bk-page)", css, StringComparison.Ordinal);
        Assert.Contains(".bk-filters", css, StringComparison.Ordinal);
        Assert.Contains("z-index: 62", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Lobsy_coach_pill_leaves_assistant_tab_clickable_only_on_button()
    {
        var root = TestRepo.FindRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/lobsy-coach-pill.css"));
        Assert.Contains(".lobsy-assistant-tab {\n  pointer-events: none;", css, StringComparison.Ordinal);
        Assert.Contains(".lobsy-assistant-tab__btn {\n  pointer-events: auto;", css, StringComparison.Ordinal);
    }
}
