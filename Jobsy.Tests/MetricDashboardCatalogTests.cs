using Jobsy.Web.Dashboard;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public sealed class MetricDashboardCatalogTests
{
    [Fact]
    public void GroupPresent_orders_known_categories_and_hides_empty()
    {
        var metrics = new List<MetricCount>
        {
            new() { Key = "errors", Label = "Errors", Value = 2 },
            new() { Key = "shares", Label = "Shares", Value = 4 },
            new() { Key = "active_vacancies", Label = "Actief", Value = 10 },
            new() { Key = "reengagement_emails_sent", Label = "Mails", Value = 1 }
        };

        var groups = MetricDashboardCatalog.GroupPresent(metrics, m => m.Key);

        Assert.Equal(new[] { "growth", "engagement", "marketing", "system" }, groups.Select(g => g.Category.Id));
        Assert.Equal("active_vacancies", groups[0].Metrics[0].Key);
        Assert.Contains(groups.Last().Metrics, m => m.Key == "errors");
    }

    [Fact]
    public void HeroMetrics_prefers_core_platform_keys()
    {
        var metrics = new List<MetricCount>
        {
            new() { Key = "likes", Label = "Likes", Value = 1 },
            new() { Key = "applications", Label = "Apps", Value = 3 },
            new() { Key = "active_vacancies", Label = "Actief", Value = 9 },
            new() { Key = "clicks", Label = "Clicks", Value = 5 }
        };

        var hero = MetricDashboardCatalog.HeroMetrics(metrics, m => m.Key, m => m.Value, max: 3);

        Assert.Equal(new[] { "active_vacancies", "applications", "clicks" }, hero.Select(m => m.Key));
    }

    [Fact]
    public void HeroMetrics_always_surfaces_nonzero_errors()
    {
        var metrics = new List<MetricCount>
        {
            new() { Key = "active_vacancies", Label = "Actief", Value = 9 },
            new() { Key = "companies_employers", Label = "Bedrijven", Value = 4 },
            new() { Key = "users_active", Label = "Users", Value = 8 },
            new() { Key = "applications", Label = "Apps", Value = 3 },
            new() { Key = "clicks", Label = "Clicks", Value = 5 },
            new() { Key = "errors", Label = "Errors", Value = 2 }
        };

        var hero = MetricDashboardCatalog.HeroMetrics(metrics, m => m.Key, m => m.Value, max: 4);

        Assert.Equal(4, hero.Count);
        Assert.Contains(hero, m => m.Key == "errors");
        Assert.True(MetricDashboardCatalog.IsWarning("errors", hero.First(m => m.Key == "errors").Value));
    }

    [Fact]
    public void CategoryIdFor_resolves_other_bucket_from_groups()
    {
        var metrics = new List<MetricCount>
        {
            new() { Key = "active_vacancies", Label = "Actief", Value = 1 },
            new() { Key = "custom_kpi", Label = "Custom", Value = 7 }
        };

        var groups = MetricDashboardCatalog.GroupPresent(metrics, m => m.Key);

        Assert.Equal("growth", MetricDashboardCatalog.CategoryIdFor("active_vacancies", groups, m => m.Key));
        Assert.Equal(MetricDashboardCatalog.OtherCategoryId, MetricDashboardCatalog.CategoryIdFor("custom_kpi", groups, m => m.Key));
        Assert.Null(MetricDashboardCatalog.CategoryIdFor("custom_kpi"));
    }

    [Fact]
    public void IsWarning_only_for_nonzero_errors()
    {
        Assert.True(MetricDashboardCatalog.IsWarning("errors", 1));
        Assert.False(MetricDashboardCatalog.IsWarning("errors", 0));
        Assert.False(MetricDashboardCatalog.IsWarning("applications", 99));
    }

    [Fact]
    public void CategoryHasWarning_detects_error_tile()
    {
        var metrics = new List<MetricCount>
        {
            new() { Key = "unpublished_vacancies", Label = "Concepten", Value = 1 },
            new() { Key = "errors", Label = "Errors", Value = 3 }
        };

        Assert.True(MetricDashboardCatalog.CategoryHasWarning(metrics, m => m.Key, m => m.Value));
        Assert.False(MetricDashboardCatalog.CategoryHasWarning(
            metrics.Where(m => m.Key != "errors").ToList(),
            m => m.Key,
            m => m.Value));
    }
}
