namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase8Tests
{
    [Fact]
    public void Ouder_portal_uses_ssr_endpoints_not_blazor_page()
    {
        var root = RepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Public/Ouder.razor")));

        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/ParentPortalEndpoints.cs"));
        Assert.Contains("MapGet(OuderSsrHtml.PagePath", endpoints, StringComparison.Ordinal);

        var html = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/OuderSsrHtml.cs"));
        Assert.Contains("PupilG78PassportSsrHtml", html, StringComparison.Ordinal);
        Assert.Contains("ouder-downloads", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\\\"ouder-login\\\"", html, StringComparison.Ordinal);
        Assert.Contains("ll-topbar", html, StringComparison.Ordinal);
        Assert.DoesNotContain("_framework/blazor", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<input type=\"text\"", html, StringComparison.Ordinal);

        var auth = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Auth/ParentPassportAuthEndpoints.cs"));
        Assert.Contains("MapPost(\"/ouder/kijk\"", auth, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/ouder/uitloggen\"", auth, StringComparison.Ordinal);

        var pdf = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/ParentPassportPdfEndpoints.cs"));
        Assert.Contains("/ouder/pdf", pdf, StringComparison.Ordinal);
        Assert.Contains("/ouder/antwoorden/pdf", pdf, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapParentPortalEndpoints", program, StringComparison.Ordinal);
        var mapRazorIdx = program.IndexOf("app.MapRazorComponents", StringComparison.Ordinal);
        var mapOuderIdx = program.IndexOf("MapParentPortalEndpoints", StringComparison.Ordinal);
        Assert.True(mapOuderIdx > 0 && mapRazorIdx > mapOuderIdx, "Parent portal endpoints must register before MapRazorComponents.");
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
