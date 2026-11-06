using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

/// <summary>TeaserLayout funnel link fixes (D16).</summary>
public class TeaserLayoutFunnelTests
{
    [Fact]
    public void Source_uses_public_shell_and_employer_register_cta()
    {
        var root = FindRepoRoot();
        var teaser = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "TeaserLayout.razor"));
        var header = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "Public", "PublicHeader.razor"));

        Assert.Contains("PublicHeader", teaser, StringComparison.Ordinal);
        Assert.Contains("TeaserCampaign=\"true\"", teaser, StringComparison.Ordinal);
        Assert.Contains("PublicFooter", teaser, StringComparison.Ordinal);
        Assert.Contains("InitializeFromRequest", teaser, StringComparison.Ordinal);

        Assert.Contains("TeaserCampaign", header, StringComparison.Ordinal);
        Assert.Contains("PublicRoutes.CompanyRegister", header, StringComparison.Ordinal);
        Assert.Contains("PublicRoutes.Banenkaart", header, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/westland\"", header, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicRoutes_constants_match_spec()
    {
        Assert.Equal("/", PublicRoutes.Landing);
        Assert.Equal("/account-maken", PublicRoutes.CreateAccount);
        Assert.Equal("/ontdek", PublicRoutes.Test);
        Assert.Equal("/banenkaart", PublicRoutes.Banenkaart);
        Assert.Equal("/contact", PublicRoutes.Contact);
        Assert.Equal("/melden", PublicRoutes.ReportContent);
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
