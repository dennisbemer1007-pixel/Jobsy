namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase4Tests
{
    [Fact]
    public void Teacher_overview_and_code_detail_use_ssr_endpoints_not_blazor_pages()
    {
        var root = RepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarKlasOverview.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarCodeDetail.razor")));

        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/TeacherPortalOverviewEndpoints.cs"));
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}/code/{codeId:guid}\"", endpoints, StringComparison.Ordinal);

        var overviewHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/TeacherKlasOverviewSsrHtml.cs"));
        Assert.Contains("BuildBody", overviewHtml, StringComparison.Ordinal);
        Assert.Contains("leraar-overview-charts", overviewHtml, StringComparison.Ordinal);
        Assert.Contains("testvenster", overviewHtml, StringComparison.Ordinal);
        Assert.Contains("codelist.pdf", overviewHtml, StringComparison.Ordinal);

        var detailHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/TeacherCodeDetailSsrHtml.cs"));
        Assert.Contains("BuildBody", detailHtml, StringComparison.Ordinal);
        Assert.Contains("report.pdf", detailHtml, StringComparison.Ordinal);

        var pdfEndpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/TeacherPupilReportPdfEndpoints.cs"));
        Assert.Contains("report.pdf", pdfEndpoints, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapTeacherPortalOverviewEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapTeacherPupilReportPdfEndpoints", program, StringComparison.Ordinal);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
