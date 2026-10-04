using Jobsy.Core.Rules;
using Jobsy.Web.Admin;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class AdminRun5Tests
{
    private static string Text(string key) => key switch
    {
        "AdminAudit.Target.Retention" => "Bewaartermijn",
        "AdminSettings.Schools.Enabled.Title" => "Scholen-portalen actief",
        "AdminAction.Resource.AdminSearch" => "Beheer-zoekopdracht",
        "AdminLogs.Message.SchoolRetention" => "Bewaartermijn scholen uitgevoerd",
        _ => key
    };

    [Fact]
    public void Only_the_most_specific_sidebar_item_is_active()
    {
        AssertPair("/admin/beveiliging", "/admin/beveiliging/gegevensinzage");
        AssertPair("/admin/beveiliging", "/admin/beveiliging/2fa");
        AssertPair("/admin/beveiliging", "/admin/beveiliging/systeemlogs");
        AssertPair("/admin/gebruikers", "/admin/gebruikers/rollen");
        AssertPair("/admin/vacatures", "/admin/vacatures/ats");
        AssertPair("/admin/financien", "/admin/financien/prijzen");
    }

    [Fact]
    public void Stored_keys_render_as_dutch_labels_and_search_maps_back()
    {
        Assert.Equal("Scholen-portalen actief", AdminActionLabels.Target("SchoolsEnabled", Text));
        Assert.Equal("AdminAction.Resource.AdminSearch", AdminActionLabels.Resource("admin.search", key => key));
        Assert.Equal("Beheer-zoekopdracht", AdminActionLabels.Resource("admin.search", Text));
        Assert.Equal("Bewaartermijn scholen uitgevoerd", AdminActionLabels.LogMessage("school.retention.run", Text));
        Assert.Contains("Data retention", AdminActionLabels.RawKeysForQuery("Bewaartermijn", Text));
        Assert.Contains("SchoolsEnabled", AdminActionLabels.RawKeysForQuery("Scholen-portalen", Text));
        Assert.DoesNotContain("spend costs (InsightsUnlock)", File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Localization", "UiStringsAdmin.cs")));
    }

    [Fact]
    public void Hospital_menu_titles_are_not_vacancies_and_versions_only_increase()
    {
        Assert.False(AtsListingValidation.TryValidateForReview(
            "Specialismen en poliklinieken",
            "HagaZiekenhuis",
            "Bekijk onze specialismen en solliciteer op een functie bij het ziekenhuis.",
            out _));

        var app = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("js/app-core.js?v=20261004-run8c", app, StringComparison.Ordinal);
        Assert.Contains("css/features/admin.css?v=20261004-08", app, StringComparison.Ordinal);
        Assert.Contains("css/app.min.css?v=20261004-run8c", app, StringComparison.Ordinal);
        Assert.True(string.CompareOrdinal("20261004-run5d", "20261004-run4b") > 0);
        Assert.True(string.CompareOrdinal("20261004-run5d", "20261004-run4") > 0);
        Assert.True(string.CompareOrdinal("20261004-run7m", "20261004-readaloud3") > 0);
        Assert.True(string.CompareOrdinal("20261004-run7m", "20261004-readaloud2") > 0);
        Assert.True(string.CompareOrdinal("20261004-10", "20261004-09") > 0);
        Assert.True(string.CompareOrdinal("20261004-09", "20261004-08") > 0);
    }

    [Fact]
    public void Open_link_sets_the_admin_drawer_query()
    {
        var href = AdminOpenLink.WithOpen("/admin/vacatures?status=Active", Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Assert.Contains("open=11111111-1111-1111-1111-111111111111", href, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status=Active", href, StringComparison.Ordinal);
        Assert.DoesNotContain("/vacancies/", href, StringComparison.Ordinal);
    }

    private static void AssertPair(string parentHref, string childHref)
    {
        var items = AdminNav.Groups.SelectMany(g => g.Items).ToList();
        var parent = items.Single(i => i.Href == parentHref);
        var child = items.Single(i => i.Href == childHref);
        Assert.True(AdminNav.Matches(childHref, child));
        Assert.False(AdminNav.Matches(childHref, parent));
        Assert.True(AdminNav.Matches(parentHref, parent));
        Assert.False(AdminNav.Matches(parentHref, child));
    }

    private static string FindRoot()
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
