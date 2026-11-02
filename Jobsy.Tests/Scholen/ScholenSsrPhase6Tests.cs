namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase6Tests
{
    [Fact]
    public void School_admin_portal_uses_ssr_endpoints_not_blazor_pages()
    {
        var root = RepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/School/SchoolDashboard.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/School/SchoolClasses.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/School/SchoolClassDetail.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/School/SchoolClassForm.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/School/TestWindowDialog.razor")));

        var getEndpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/SchoolPortalEndpoints.cs"));
        Assert.Contains("MapGet(\"/school\"", getEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/school/klassen/nieuw\"", getEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/school/klassen/{classId:guid}\"", getEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/school/resultaten\"", getEndpoints, StringComparison.Ordinal);

        var formEndpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/SchoolPortalFormEndpoints.cs"));
        Assert.Contains("MapPost(\"/school/klassen\"", formEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/school/klassen/{classId:guid}/test-window\"", formEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/school/privacy/delete-year\"", formEndpoints, StringComparison.Ordinal);
        Assert.Contains("Roles = JobsyRoles.SchoolAdmin", formEndpoints, StringComparison.Ordinal);

        var fileEndpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/SchoolCodeListFileEndpoints.cs"));
        Assert.Contains("MapGet(\"/school/klassen/{classId:guid}/codelist.pdf\"", fileEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/school/klassen/{classId:guid}/codelist.csv\"", fileEndpoints, StringComparison.Ordinal);

        var html = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/SchoolPortalSsrHtml.cs"));
        var helpers = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/SchoolPortalSsrHelpers.cs"));
        Assert.Contains("ent-kpi-card", helpers, StringComparison.Ordinal);
        Assert.Contains("ent-data-table", html, StringComparison.Ordinal);
        Assert.Contains("school-test-window-closes", html, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapSchoolPortalEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapSchoolPortalFormEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapSchoolCodeListFileEndpoints", program, StringComparison.Ordinal);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
