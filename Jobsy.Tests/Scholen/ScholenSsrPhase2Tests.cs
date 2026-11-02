namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase2Tests
{
    [Fact]
    public void Leerling_login_and_leraar_entry_use_ssr_endpoints_not_blazor_pages()
    {
        var root = RepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leerling/LeerlingLogin.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leraar/LeraarDashboard.razor")));
        var login = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PupilLoginPageEndpoints.cs"));
        var leraar = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/TeacherPortalEntryEndpoints.cs"));
        var html = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilLeerlingLoginSsrHtml.cs"));
        Assert.Contains("MapGet(PagePath", login, StringComparison.Ordinal);
        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapPupilLoginPageEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapPupilAuthEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapGet(PagePath", leraar, StringComparison.Ordinal);
        Assert.DoesNotContain("blazor.web", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("leerling-login.js", html, StringComparison.Ordinal);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
