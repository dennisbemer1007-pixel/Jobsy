namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase7Tests
{
    [Fact]
    public void Leerling_journey_uses_ssr_endpoints_not_blazor_pages()
    {
        var root = RepoRoot();
        var leerlingPages = new[]
        {
            "Jobsy.Web/Components/Pages/Leerling/LeerlingDitBenJij.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingEiland.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingPaspoort.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingPuzzel.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingReis.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingStart.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingStop.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingVakken.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingVoPaspoort.razor",
            "Jobsy.Web/Components/Pages/Leerling/LeerlingWereld.razor",
            "Jobsy.Web/Components/Pages/Leerling/MboHbo/LeerlingMboHboOverzicht.razor",
            "Jobsy.Web/Components/Pages/Leerling/MboHbo/LeerlingMboHboPaspoort.razor",
            "Jobsy.Web/Components/Pages/Leerling/MboHbo/LeerlingMboHboTest.razor",
            "Jobsy.Web/Components/Pages/Leerling/MboHbo/LeerlingMboHboUitkomst.razor",
            "Jobsy.Web/Components/Pages/Leerling/MboHbo/LeerlingMboHboUitleg.razor",
        };

        foreach (var rel in leerlingPages)
        {
            Assert.False(File.Exists(Path.Combine(root, rel)), rel);
        }

        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PupilLeerlingJourneyEndpoints.cs"));
        Assert.Contains("MapGet(\"/leerling/start\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/wereld\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/reis\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/eiland\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/vakken\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/puzzel/{key}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/dit-ben-jij\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/paspoort\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leerling/vo-paspoort\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ReisAnswerPath", endpoints, StringComparison.Ordinal);
        Assert.Contains("WereldSavePath", endpoints, StringComparison.Ordinal);
        Assert.Contains("EilandSavePath", endpoints, StringComparison.Ordinal);
        Assert.Contains("VakkenSavePath", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(ReisAnswerPath", endpoints, StringComparison.Ordinal);

        var reisHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilReisSsrHtml.cs"));
        var parts = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilLeerlingSsrParts.cs"));
        Assert.Contains("data-ll-answer-scale", parts, StringComparison.Ordinal);
        Assert.Contains("ll-rail", reisHtml, StringComparison.Ordinal);

        var paspoortHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilPaspoortSsrHtml.cs"));
        Assert.Contains("PupilG78PassportSsrHtml", paspoortHtml, StringComparison.Ordinal);
        Assert.Contains("lp-g78-paspoort-actions", paspoortHtml, StringComparison.Ordinal);

        var frame = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilLeerlingSsrFrame.cs"));
        Assert.DoesNotContain("_framework/blazor", frame, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("leerling-journey-ssr.js", frame, StringComparison.Ordinal);
        Assert.Contains("read-aloud.js", frame, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/js/leerling-journey-ssr.js")));

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapPupilLeerlingJourneyEndpoints", program, StringComparison.Ordinal);
        var mapRazorIdx = program.IndexOf("app.MapRazorComponents", StringComparison.Ordinal);
        var mapJourneyIdx = program.IndexOf("MapPupilLeerlingJourneyEndpoints", StringComparison.Ordinal);
        Assert.True(mapJourneyIdx > 0 && mapRazorIdx > mapJourneyIdx, "Journey endpoints must register before MapRazorComponents.");
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
