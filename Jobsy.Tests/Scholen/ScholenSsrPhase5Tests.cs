namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase5Tests
{
    [Fact]
    public void Teacher_codes_and_test_window_use_ssr_endpoints_not_blazor_pages()
    {
        var root = RepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarCodes.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarTestWindow.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/School/TeacherClassOverviewCharts.razor")));

        var codesEndpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/TeacherPortalCodesEndpoints.cs"));
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}/codes\"", codesEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/leraar/klas/{classId:guid}/codes/add\"", codesEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/leraar/klas/{classId:guid}/codes/{codeId:guid}/replace\"", codesEndpoints, StringComparison.Ordinal);

        var testWindowEndpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/TeacherPortalTestWindowEndpoints.cs"));
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}/testvenster\"", testWindowEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/leraar/klas/{classId:guid}/testvenster\"", testWindowEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/leraar/klas/{classId:guid}/testvenster/clear-pause\"", testWindowEndpoints, StringComparison.Ordinal);

        var codesHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/TeacherCodesSsrHtml.cs"));
        Assert.Contains("BuildBody", codesHtml, StringComparison.Ordinal);
        Assert.Contains("leraar-code-table", codesHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"q\"", codesHtml.Replace("\\\"", "\"", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("status=", codesHtml, StringComparison.Ordinal);

        var testHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/TeacherTestWindowSsrHtml.cs"));
        Assert.Contains("BuildBody", testHtml, StringComparison.Ordinal);
        Assert.Contains("school-test-window-closes", testHtml, StringComparison.Ordinal);
        Assert.Contains("clear-pause", testHtml, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapTeacherPortalCodesEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapTeacherPortalTestWindowEndpoints", program, StringComparison.Ordinal);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
