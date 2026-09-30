using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

/// <summary>TeaserLayout funnel link fixes (D16).</summary>
public class TeaserLayoutFunnelTests
{
    [Fact]
    public void Source_links_home_and_create_account_not_register_or_westland()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "TeaserLayout.razor");
        var text = File.ReadAllText(path);
        Assert.Contains("PublicRoutes.Landing", text, StringComparison.Ordinal);
        Assert.Contains("PublicRoutes.CreateAccount", text, StringComparison.Ordinal);
        Assert.Contains("PublicNav.CreateAccount", text, StringComparison.Ordinal);
        Assert.Contains("InitializeFromRequest", text, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/register\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/westland\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain(">Registreren<", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicRoutes_constants_match_spec()
    {
        Assert.Equal("/", PublicRoutes.Landing);
        Assert.Equal("/account-maken", PublicRoutes.CreateAccount);
        Assert.Equal("/ontdek", PublicRoutes.Test);
        Assert.Equal("/banenkaart", PublicRoutes.Banenkaart);
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
