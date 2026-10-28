using Jobsy.Web.Hosting;

namespace Jobsy.Tests.Scholen;

public sealed class PupilDroombaanSsrTests
{
    [Fact]
    public void Droombaan_is_served_by_ssr_endpoints_not_blazor_page()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leerling/LeerlingDroombaan.razor")));
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PupilDroombaanFormEndpoints.cs"));
        var html = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilDroombaanSsrHtml.cs"));
        var frame = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilLeerlingSsrFrame.cs"));
        Assert.Contains("MapGet(PagePath", endpoints, StringComparison.Ordinal);
        Assert.Contains("SavePath", endpoints, StringComparison.Ordinal);
        Assert.Contains("name=\\\"DreamJob\\\"", html, StringComparison.Ordinal);
        Assert.Contains("ll-dream-ssr__form", html, StringComparison.Ordinal);
        Assert.DoesNotContain("LeerlingSpeak", html, StringComparison.Ordinal);
        Assert.DoesNotContain("LeerlingCoachCue", html, StringComparison.Ordinal);
        Assert.DoesNotContain("_framework/blazor", frame, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blazor.web", frame, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Paspoort_actions_use_new_static_bar_not_ll_result_actions()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var paspoort = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Leerling/LeerlingPaspoort.razor"));
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/leerling-paspoort-g78.css"));
        Assert.Contains("lp-g78-paspoort-actions", paspoort, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"leerling-paspoort-actions\"", paspoort, StringComparison.Ordinal);
        Assert.DoesNotContain("ll-result__actions", paspoort, StringComparison.Ordinal);
        Assert.Contains(".lp-g78-paspoort-actions", css, StringComparison.Ordinal);
        Assert.Contains("position: static", css, StringComparison.Ordinal);
    }
}
