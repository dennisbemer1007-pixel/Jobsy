using Jobsy.Core.Rules;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class DreamJobCatalogTests
{
    [Fact]
    public void Catalog_has_unique_entries_with_renderable_icons()
    {
        Assert.True(DreamJobCatalog.All.Count >= 40);
        Assert.Equal(DreamJobCatalog.All.Count, DreamJobCatalog.All.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(DreamJobCatalog.All.Count, DreamJobCatalog.All.Select(x => x.TitleNl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(DreamJobCatalog.All, job => Assert.False(string.IsNullOrWhiteSpace(DreamJobIcons.TryGet(job.IconKey))));
    }

    [Fact]
    public void Search_prioritizes_prefix_and_ignores_case_and_diacritics()
    {
        Assert.Equal("dierenarts", DreamJobCatalog.Search("DIER")[0].Key);
        Assert.Equal("dierenarts", DreamJobCatalog.Search("díér")[0].Key);
        Assert.Contains(DreamJobCatalog.Search("dier"), item => item.Key == "dierenverzorger");
    }
}
