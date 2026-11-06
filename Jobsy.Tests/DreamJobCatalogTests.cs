using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class DreamJobCatalogTests
{
    [Fact]
    public void Catalog_has_unique_entries_with_renderable_icons()
    {
        Assert.True(DreamJobCatalog.All.Count >= 58);
        Assert.Equal(DreamJobCatalog.All.Count, DreamJobCatalog.All.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(DreamJobCatalog.All.Count, DreamJobCatalog.All.Select(x => x.TitleNl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(DreamJobCatalog.All, job => Assert.False(string.IsNullOrWhiteSpace(DreamJobIcons.TryGet(job.IconKey))));
        Assert.Contains(DreamJobCatalog.All, j => j.Key == "logistiek-medewerker");
        Assert.Equal("logistiek-medewerker", DreamJobCatalog.Search("magazijn")[0].Key);
        Assert.Equal("horecamedewerker", DreamJobCatalog.All[^1].Key);
    }

    [Fact]
    public void Catalog_keys_have_localized_titles()
    {
        foreach (var job in DreamJobCatalog.All)
        {
            var key = $"DreamJob.{job.Key}";
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get(key, "nl")), key);
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get(key, "en")), key);
        }

        Assert.Equal("Logistiek medewerker", UiStrings.Get("DreamJob.logistiek-medewerker", "nl"));
        Assert.Equal("Logistics worker", UiStrings.Get("DreamJob.logistiek-medewerker", "en"));
        Assert.NotEqual(
            UiStrings.Get("DreamJob.verkoper", "nl"),
            UiStrings.Get("DreamJob.verkoper", "en"));
    }

    [Fact]
    public void Search_prioritizes_prefix_and_ignores_case_and_diacritics()
    {
        Assert.Equal("dierenarts", DreamJobCatalog.Search("DIER")[0].Key);
        Assert.Equal("dierenarts", DreamJobCatalog.Search("díér")[0].Key);
        Assert.Contains(DreamJobCatalog.Search("dier"), item => item.Key == "dierenverzorger");
    }
}
