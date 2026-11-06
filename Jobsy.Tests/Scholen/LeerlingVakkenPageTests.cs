namespace Jobsy.Tests.Scholen;

public class LeerlingVakkenPageTests
{
    [Fact]
    public void Vakken_page_binds_page_query_via_SupplyParameterFromQuery()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var src = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Leerling", "LeerlingVakken.razor"));
        Assert.Contains("[SupplyParameterFromQuery(Name = \"page\")]", src, StringComparison.Ordinal);
        Assert.Contains("LeerlingScene", src, StringComparison.Ordinal);
        Assert.Contains("ll-reis ll-vakken", src, StringComparison.Ordinal);
    }
}
