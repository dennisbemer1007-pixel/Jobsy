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

        var hero = MetricDashboardCatalog.HeroMetrics(metrics, m => m.Key, max: 3);

        Assert.Equal(new[] { "active_vacancies", "applications", "clicks" }, hero.Select(m => m.Key));
    }

    [Fact]
    public void IsWarning_only_for_nonzero_errors()
    {
        Assert.True(MetricDashboardCatalog.IsWarning("errors", 1));
        Assert.False(MetricDashboardCatalog.IsWarning("errors", 0));
        Assert.False(MetricDashboardCatalog.IsWarning("applications", 99));
    }
}
