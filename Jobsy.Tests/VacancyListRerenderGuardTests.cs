namespace Jobsy.Tests;

public class VacancyListRerenderGuardTests
{
    [Fact]
    public void Discovery_renders_VacancyCard_with_key_and_no_server_hover()
    {
        var root = FindRepoRoot();
        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        var cardPath = Path.Combine(root, "Jobsy.Web", "Components", "Discovery", "VacancyCard.razor");
        Assert.True(File.Exists(cardPath), "VacancyCard.razor must exist");

        var card = File.ReadAllText(cardPath);
        Assert.Contains("<VacancyCard", discovery, StringComparison.Ordinal);
        Assert.Contains("@key=\"vacancy.Id\"", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("@onmouseenter", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("is-hovered", discovery, StringComparison.Ordinal);

        Assert.Contains("data-vacancy-id", card, StringComparison.Ordinal);
        Assert.Contains("ShouldRender", card, StringComparison.Ordinal);
        Assert.DoesNotContain("@onmouseenter", card, StringComparison.Ordinal);
        Assert.DoesNotContain("@onmouseleave", card, StringComparison.Ordinal);

        var hoverJs = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "discoveryHover.js"));
        Assert.Contains("jobMap.highlight", hoverJs, StringComparison.Ordinal);
        Assert.Contains("root: root", hoverJs, StringComparison.Ordinal);
        Assert.Contains("vacancy-list", hoverJs, StringComparison.Ordinal);
        Assert.Contains("discoveryHover.js?v=20260928-list-rerender", discovery, StringComparison.Ordinal);
        Assert.Contains("jobsyDiscovery.observeMore", discovery, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
