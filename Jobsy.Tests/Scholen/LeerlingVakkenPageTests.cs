namespace Jobsy.Tests.Scholen;

public class LeerlingVakkenPageTests
{
    [Fact]
    public void Vakken_ssr_binds_page_query_and_scene_markup()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var src = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Scholen", "PupilVakkenSsrHtml.cs"));
        Assert.Contains("name=\\\"Page\\\"", src, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"leerling-vakken\"", src, StringComparison.Ordinal);
        Assert.Contains("ll-reis ll-vakken", src, StringComparison.Ordinal);
    }
}
