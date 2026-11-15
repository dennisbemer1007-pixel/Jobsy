using System.Net;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Web.Hosting;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkFunctieTotaalbeeldHtmlTests
{
    private const string OrderpickerEsco = "cd94def5-3442-4c2e-ae3d-0761a3008bcb";

    [Fact]
    public void Totaalbeeld_renders_each_competence_bar_once()
    {
        var shared = Enumerable.Range(0, WerkgeverApkAnonymity.MinGroupSize)
            .Select(i => ((string?)OrderpickerEsco, State(i)))
            .ToList();
        var insights = WerkgeverApkFunctieGroupInsights.Aggregate(
            shared,
            OrderpickerEsco,
            SchoolLevel.Mbo3);
        Assert.True(insights.Visible);

        var html = WerkgeverApkFunctieMockupPages.Totaalbeeld(insights, "Demo Logistiek B.V.", "Team Logistiek");

        foreach (var bar in insights.Bars)
        {
            var needle = $"""<div class="lab">{WebUtility.HtmlEncode(bar.Label)}</div>""";
            Assert.Equal(1, CountOccurrences(html, needle));
        }
    }

    [Fact]
    public void Totaalbeeld_keeps_scholingskansen_aside_sibling_of_comp_card()
    {
        var shared = Enumerable.Range(0, WerkgeverApkAnonymity.MinGroupSize)
            .Select(i => ((string?)OrderpickerEsco, State(i)))
            .ToList();
        var insights = WerkgeverApkFunctieGroupInsights.Aggregate(
            shared,
            OrderpickerEsco,
            SchoolLevel.Mbo3);
        var html = WerkgeverApkFunctieMockupPages.Totaalbeeld(insights, "Demo Logistiek B.V.", "Team Logistiek");

        const string marker = "</section>\n          <aside>";
        Assert.Contains(marker, html, StringComparison.Ordinal);
        var compIdx = html.IndexOf("class=\"card gold-top comp-card\"", StringComparison.Ordinal);
        var layoutIdx = html.IndexOf("fn-layout wide-side", compIdx, StringComparison.Ordinal);
        Assert.True(compIdx > layoutIdx);
        var asideIdx = html.IndexOf(marker, compIdx, StringComparison.Ordinal);
        Assert.True(asideIdx > compIdx);
        var compRegion = html[compIdx..asideIdx];
        Assert.DoesNotContain("Scholingskansen voor dit team", compRegion, StringComparison.Ordinal);
        var asideRegion = html[asideIdx..];
        Assert.Contains("Scholingskansen voor dit team", asideRegion, StringComparison.Ordinal);
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

    private static PupilMboHboProgressState State(int i)
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers[q.Id] = 3 + (i % 2);
            }
        }

        return PupilMboHboProgressState.Parse(
            System.Text.Json.JsonSerializer.Serialize(new { answers }));
    }
}