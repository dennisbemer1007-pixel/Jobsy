namespace Jobsy.Tests.Scholen;

public sealed class PupilDroombaanSsrTests
{
    [Fact]
    public void Droombaan_page_is_static_ssr_without_blazor_circuit()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Leerling/LeerlingDroombaan.razor"));
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
        Assert.Contains("data-enhance=\"false\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Droombaan_form_posts_to_dedicated_save_path()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PupilDroombaanFormEndpoints.cs"));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Leerling/LeerlingDroombaan.razor"));
        Assert.Contains("\"/leerling/droombaan/save\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("PupilDroombaanFormEndpoints.Path", page, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/leerling/droombaan\"", endpoints, StringComparison.Ordinal);
    }
}
