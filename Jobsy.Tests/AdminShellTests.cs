using Jobsy.Core.Hosting;
using Jobsy.Tests.Uat;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class AdminNavTests
{
    [Fact]
    public void Groups_follow_IA_order_and_available_items_have_unique_labels()
    {
        var keys = AdminNav.Groups.Select(g => g.Key).ToArray();
        Assert.Equal(
            ["overview", "users", "orgs", "candidates", "vacancies", "finance", "content", "settings", "security"],
            keys);

        var labels = AdminNav.AvailableItems().Select(i => i.LabelKey).ToList();
        Assert.Equal(labels.Count, labels.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(AdminNav.AvailableItems(), i => i.Href == "/");
        Assert.Contains(AdminNav.AvailableItems(), i => i.Href == "/admin");
        Assert.Contains(AdminNav.AvailableItems(), i => i.Href == "/admin/te-doen");
        Assert.Contains(AdminNav.AvailableItems(), i => i.Href == "/admin/vacatures/moderatie");
    }

    [Theory]
    [InlineData("/admin", "AdminNav.Dashboard")]
    [InlineData("/admin/gebruikers", "AdminNav.AllUsers")]
    [InlineData("/admin/users", "AdminNav.AllUsers")]
    [InlineData("/admin/vacatures/categorieen", "AdminNav.CategoriesWages")]
    [InlineData("/admin/wages", "AdminNav.CategoriesWages")]
    [InlineData("/admin/beveiliging/systeemlogs", "AdminNav.SystemLogs")]
    [InlineData("/admin/logging", "AdminNav.SystemLogs")]
    public void Resolve_matches_href_alias_and_prefix(string path, string labelKey)
    {
        var resolved = AdminNav.Resolve(path);
        Assert.NotNull(resolved);
        Assert.Equal(labelKey, resolved!.Value.Item.LabelKey);
    }

    [Fact]
    public void Crumbs_are_beheer_group_item()
    {
        var crumbs = AdminNav.Crumbs("/admin/gebruikers");
        Assert.Equal(3, crumbs.Count);
        Assert.Equal("AdminShell.Beheer", crumbs[0].LabelKey);
        Assert.Equal("AdminNav.Group.Users", crumbs[1].LabelKey);
        Assert.Equal("AdminNav.AllUsers", crumbs[2].LabelKey);
        Assert.True(crumbs[2].IsCurrent);
    }

    [Fact]
    public void Every_available_href_is_a_routable_admin_page()
    {
        var routes = RazorRouteIndex.Load();
        foreach (var item in AdminNav.AvailableItems())
        {
            var page = routes.Find(item.Href);
            Assert.NotNull(page);
            Assert.Contains("Admin", page!.Roles, StringComparer.OrdinalIgnoreCase);
        }
    }
}

public class AdminLegacyRoutesTests
{
    [Theory]
    [InlineData("/admin/users", "/admin/gebruikers")]
    [InlineData("/admin/users?companyId=x", "/admin/gebruikers?companyId=x")]
    [InlineData("/admin/ambassadeurs", "/admin/gebruikers/sales?tab=ambassadeurs")]
    [InlineData("/admin/wages?x=1", "/admin/vacatures/categorieen?tab=salaris&x=1")]
    [InlineData("/admin/moderation", "/admin/vacatures/moderatie")]
    [InlineData("/admin/notifications", "/admin/content/emails")]
    [InlineData("/admin/cockpit", "/admin")]
    [InlineData("/admin/sales", "/admin/financien/prijzen?tab=sales")]
    [InlineData("/admin/financien/uitbetalingen?tab=goodwill", "/admin/financien/goodwill")]
    public void TryMap_preserves_query_and_tab(string from, string expected)
    {
        Assert.True(AdminLegacyRoutes.TryMap(from, out var dest));
        Assert.Equal(expected, dest);
    }

    [Fact]
    public void Non_admin_paths_are_untouched()
    {
        Assert.False(AdminLegacyRoutes.TryMap("/home", out _));
        Assert.False(AdminLegacyRoutes.TryMap("/candidate/profile", out _));
    }
}

public class DeploymentEnvironmentTests
{
    [Theory]
    [InlineData("https://acceptatie.lobsy.nl", null, "Acceptatie")]
    [InlineData("https://lobsy.nl", null, "Productie")]
    [InlineData("https://www.lobsy.nl", null, "Productie")]
    [InlineData("http://localhost:5201", null, "Lokaal")]
    [InlineData(null, null, "Lokaal")]
    [InlineData("https://lobsy.nl", "Acceptatie", "Acceptatie")]
    [InlineData(null, "Production", "Productie")]
    [InlineData(null, "Acceptatie", "Acceptatie")]
    [InlineData("", null, "Acceptatie", "acceptatie.lobsy.nl")]
    [InlineData("", null, "Acceptatie", "lobsy-acc-web.onrender.com")]
    public void Resolve_from_public_web_base_url(
        string? url,
        string? overrideLabel,
        string expected,
        string? requestHost = null)
        => Assert.Equal(expected, DeploymentEnvironment.Resolve(url, overrideLabel, requestHost));
}

public class RoleNavCatalogSnapshotTests
{
    [Fact]
    public void Admin_catalog_is_empty_and_other_catalogs_are_unchanged()
    {
        Assert.Empty(RoleNavCatalog.Admin);
        // Employer bottom-nav catalogs empty — WerkgeverNav owns chrome (D1).
        Assert.Empty(RoleNavCatalog.Enterprise);
        Assert.Empty(RoleNavCatalog.Regional);
        Assert.Empty(RoleNavCatalog.Branch);
        Assert.Empty(RoleNavCatalog.Intermediary);
        Assert.Empty(RoleNavCatalog.SalesManager);

        Assert.Equal(
            ["/banenkaart", "/candidate/liked", "/candidate/applications", "/carriere", "/candidate/profile"],
            RoleNavCatalog.Candidate.Select(i => i.Href));
        Assert.Equal("/ambassadeur/toolkit", RoleNavCatalog.Ambassadeur[1].Href);

        var passportOn = RoleNavCatalog.CandidateItems(
            new Jobsy.Core.Features.FeatureFlagSnapshot(true, true));
        Assert.Equal(
            ["/candidate/ontdekkingsreis", "/candidate/paspoort", "/banenkaart", "/candidate/applications", "/carriere"],
            passportOn.Select(i => i.Href));
    }
}

public class AdminLegacyHrefTests
{
    private static readonly HashSet<string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "AdminLegacyRoutes.cs",
        "AdminLegacyRedirect.razor",
        "AdminNav.cs",
        "AmbassadorsFeatureMiddleware.cs", // maps legacy /admin/ambassadeurs while AmbassadorsEnabled is off
    };

    private static readonly string[] Forbidden =
    [
        "/admin/users",
        "/admin/companies",
        "/admin/vacancies",
        "/admin/settings",
        "/admin/finance",
        "/admin/tokens",
        "/admin/logging",
        "/admin/moderation",
        "/admin/notifications",
        "/admin/sales-managers",
        "/admin/ambassadeurs",
        "/admin/cnames",
        "/admin/ats-vacancies",
        "/admin/vacancy-categories",
        "/admin/wages",
        "/admin/sales",
        "/admin/token-finance",
        "/admin/about",
        "/admin/marketing-flyer",
        "/admin/training",
        "/admin/masterdata",
        "/admin/exclusivity",
        "/admin/mail-test",
        "/admin/company",
        "/admin/integrations",
        "/admin/api-keys",
        "/admin/personal-data-access-log",
        "/admin/cockpit",
    ];

    [Fact]
    public void No_internal_web_links_use_legacy_admin_urls()
    {
        var root = Path.Combine(RepoRoot.Find(), "Jobsy.Web");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var ext = Path.GetExtension(file);
            if (ext is not (".cs" or ".razor"))
            {
                continue;
            }

            var name = Path.GetFileName(file);
            if (AllowedFiles.Contains(name))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var old in Forbidden)
            {
                if (text.Contains($"\"{old}\"", StringComparison.Ordinal)
                    || text.Contains($"'{old}'", StringComparison.Ordinal)
                    || text.Contains($"href=\"{old}", StringComparison.Ordinal)
                    || text.Contains($"NavigateTo(\"{old}", StringComparison.Ordinal))
                {
                    if (name is "RoleNavCatalog.cs" && old == "/admin/tokens")
                    {
                        continue;
                    }

                    offenders.Add($"{name}: {old}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "Legacy admin URLs still referenced:\n" + string.Join("\n", offenders));
    }
}
