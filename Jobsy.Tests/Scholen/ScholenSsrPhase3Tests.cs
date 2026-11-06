namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase3Tests
{
    [Fact]
    public void Readonly_leraar_pages_use_ssr_endpoints_not_blazor_pages()
    {
        var root = RepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarGroup.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarDreamJobs.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarMaterials.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarMboHboVoortgang.razor")));

        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/TeacherPortalReadOnlyEndpoints.cs"));
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}/groep\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}/droombanen\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}/materiaal\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/leraar/klas/{classId:guid}/mbo-hbo\"", endpoints, StringComparison.Ordinal);

        var html = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/TeacherPortalReadOnlySsrHtml.cs"));
        Assert.Contains("BuildGroupBody", html, StringComparison.Ordinal);
        Assert.Contains("BuildDreamJobsBody", html, StringComparison.Ordinal);
        Assert.Contains("BuildMaterialsBody", html, StringComparison.Ordinal);
        Assert.Contains("BuildMboHboBody", html, StringComparison.Ordinal);
        Assert.Contains("codelist.pdf", html, StringComparison.Ordinal);

        var files = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/TeacherCodeListFileEndpoints.cs"));
        Assert.Contains("codelist.pdf", files, StringComparison.Ordinal);
        Assert.Contains("codelist.csv", files, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapTeacherPortalReadOnlyEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapTeacherCodeListFileEndpoints", program, StringComparison.Ordinal);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
