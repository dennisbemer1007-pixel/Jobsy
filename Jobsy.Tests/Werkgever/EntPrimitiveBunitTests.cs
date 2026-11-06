using Bunit;
using Jobsy.Web.Components.Ui.Enterprise;

namespace Jobsy.Tests.Werkgever;

public class EntPrimitiveBunitTests : BunitContext
{
    [Fact]
    public void EntTabs_marks_active_tab()
    {
        var cut = Render<EntTabs>(p => p
            .Add(x => x.Tabs, [new EntTabs.Tab("a", "A"), new EntTabs.Tab("b", "B")])
            .Add(x => x.ActiveKey, "b")
            .Add(x => x.BasePath, "/werkgever/x"));
        Assert.Contains("is-active", cut.Markup);
        Assert.Contains("aria-selected", cut.Markup);
    }

    [Fact]
    public void EntImpactNote_renders_child()
    {
        var cut = Render<EntImpactNote>(p => p
            .Add(x => x.Tone, "info")
            .AddChildContent("Hallo"));
        Assert.Contains("Hallo", cut.Markup);
        Assert.Contains("ent-impact-note--info", cut.Markup);
    }

    [Fact]
    public void EntKpiCard_shows_label_and_value()
    {
        var cut = Render<EntKpiCard>(p => p
            .Add(x => x.Label, "Actief")
            .Add(x => x.Value, "12"));
        Assert.Contains("Actief", cut.Markup);
        Assert.Contains("12", cut.Markup);
    }
}
