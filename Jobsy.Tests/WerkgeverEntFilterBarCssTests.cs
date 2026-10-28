namespace Jobsy.Tests;

public sealed class WerkgeverEntFilterBarCssTests
{
    [Fact]
    public void Enterprise_search_label_is_not_banenkaart_blue_button_style()
    {
        var root = TestRepo.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var ent = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/enterprise-ui.css"));
        var bar = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Ui/Enterprise/EntFilterBar.razor"));

        Assert.Contains("button.filter-bar__search", app, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"ent-filter-bar__search filter-bar__search\"", bar, StringComparison.Ordinal);
        Assert.Contains(".ent-filter-bar__search input:focus", ent, StringComparison.Ordinal);
        Assert.Contains("background: var(--surface)", ent, StringComparison.Ordinal);
    }
}
