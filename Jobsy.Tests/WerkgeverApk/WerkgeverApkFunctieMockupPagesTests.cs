using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Web.Hosting;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkFunctieMockupPagesTests
{
    [Fact]
    public void FunctieKiezen_replaces_full_suggest_block_without_template_leftovers()
    {
        var pick = new WerkgeverApkFunctiePickDto(
            EmployeeCodeId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            EmployeeLabel: "Sara de Wit",
            SelectedEscoOccupationId: "cd94def5-3442-4c2e-ae3d-0761a3008bcb",
            SelectedOccupationName: "Orderpicker",
            Suggestions:
            [
                new("cd94def5-3442-4c2e-ae3d-0761a3008bcb", "Orderpicker"),
                new("040fcfa5-24be-43eb-afd5-828297175b16", "Magazijnmedewerker kleding"),
                new("45037d43-a8f5-4f46-b332-b2935bc305f4", "Vrachtwagenchauffeur"),
            ],
            Query: "orderp",
            TeamRows: []);

        var html = WerkgeverApkFunctieMockupPages.FunctieKiezen(
            pick,
            "Team Logistiek",
            "Demo Logistiek B.V.",
            "/post",
            "/search",
            antiforgery: "");

        Assert.DoesNotContain("""<div class="sug"><span class="mark"></span><b>Magazijnmedewerker</b></div>""", html, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(html, "<b>Orderpicker</b>"));
        Assert.Contains("Gekozen:", html, StringComparison.Ordinal);
        Assert.Contains("?v=", html, StringComparison.Ordinal);
        Assert.Contains("lobsy-64.png", html, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }

        return count;
    }
}