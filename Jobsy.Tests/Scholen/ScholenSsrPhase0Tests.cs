namespace Jobsy.Tests.Scholen;

public sealed class ScholenSsrPhase0Tests
{
    [Fact]
    public void Public_school_routes_skip_blazor_runtime_where_required()
    {
        var root = RepoRoot();
        var scholen = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/ScholenPublic.razor"));
        var consent = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Public/ParentalConsent.razor"));
        Assert.Contains("[NoBlazorRuntime]", scholen, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", consent, StringComparison.Ordinal);
    }

    [Fact]
    public void School_ssr_frame_has_no_blazor_scripts()
    {
        var root = RepoRoot();
        var frame = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/ScholenPortalSsrFrame.cs"));
        var catalog = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PageAssetCatalog.cs"));
        Assert.DoesNotContain("blazor.web", frame, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("_framework/blazor", frame, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("StylesheetsForSchoolSsr", catalog, StringComparison.Ordinal);
        Assert.Contains("SchoolSsrCoreCss", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Droombaan_and_consent_confirm_stay_on_endpoints()
    {
        var root = RepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapPupilDroombaanFormEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapPupilAuthEndpoints", program, StringComparison.Ordinal);
        var tokens = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Auth/PublicTokenEndpoints.cs"));
        Assert.Contains("/toestemming/confirm", tokens, StringComparison.Ordinal);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
