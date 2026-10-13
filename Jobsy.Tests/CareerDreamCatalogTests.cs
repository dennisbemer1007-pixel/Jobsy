using Jobsy.Core.Careers;

namespace Jobsy.Tests;

public class CareerDreamCatalogTests
{
    private static readonly HashSet<string> AllowedLevels =
        new(StringComparer.Ordinal) { "Entry", "Mbo2", "Mbo3", "Mbo4", "Hbo", "Wo" };

    [Fact]
    public void Catalog_has_at_least_150_unique_keys_and_titles()
    {
        Assert.True(CareerDreamCatalog.All.Count >= 150);
        Assert.Equal(
            CareerDreamCatalog.All.Count,
            CareerDreamCatalog.All.Select(e => e.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            CareerDreamCatalog.All.Count,
            CareerDreamCatalog.All.Select(e => e.Title).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(CareerDreamCatalog.All, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Key));
            Assert.False(string.IsNullOrWhiteSpace(e.Title));
            Assert.Contains(e.Level, AllowedLevels);
            Assert.False(string.IsNullOrWhiteSpace(e.Werkveld));
            Assert.InRange(e.OccupationKeys.Length, 2, 6);
            Assert.All(e.OccupationKeys, k => Assert.False(string.IsNullOrWhiteSpace(k)));
        });
    }

    [Fact]
    public void FindByKey_returns_entry_and_is_case_insensitive()
    {
        var entry = CareerDreamCatalog.FindByKey("orderpicker");
        Assert.NotNull(entry);
        Assert.Equal("Orderpicker", entry!.Title);
        Assert.Equal("Entry", entry.Level);

        Assert.Equal(entry.Key, CareerDreamCatalog.FindByKey("ORDERPICKER")!.Key);
        Assert.Null(CareerDreamCatalog.FindByKey("bestaat-niet"));
        Assert.Null(CareerDreamCatalog.FindByKey(""));
        Assert.Null(CareerDreamCatalog.FindByKey("   "));
    }

    [Fact]
    public void FindByTitleOrAlias_is_case_and_accent_insensitive()
    {
        Assert.Equal("verpleegkundige", CareerDreamCatalog.FindByTitleOrAlias("Verpleegkundige")!.Key);
        Assert.Equal("verpleegkundige", CareerDreamCatalog.FindByTitleOrAlias("VERPLEEGKUNDIGE")!.Key);
        Assert.Equal("helpende-zorg-en-welzijn", CareerDreamCatalog.FindByTitleOrAlias("helpende zorg")!.Key);
        Assert.Equal("verzorgende-ig", CareerDreamCatalog.FindByTitleOrAlias("VIG")!.Key);
        Assert.Equal("kasmedewerker", CareerDreamCatalog.FindByTitleOrAlias("glastuinbouwmedewerker")!.Key);
        Assert.Null(CareerDreamCatalog.FindByTitleOrAlias("onbekend beroep xyz"));
    }

    [Fact]
    public void Search_matches_prefix_and_contains_and_prefers_entry_level()
    {
        var order = CareerDreamCatalog.Search("order");
        Assert.NotEmpty(order);
        Assert.Equal("orderpicker", order[0].Key);

        var zorg = CareerDreamCatalog.Search("zorg");
        Assert.NotEmpty(zorg);
        Assert.True(zorg.Count <= 8);
        Assert.Contains(zorg, e => e.Key == "zorghulp");

        var empty = CareerDreamCatalog.Search("   ");
        Assert.Empty(empty);

        var limited = CareerDreamCatalog.Search("medewerker", max: 3);
        Assert.True(limited.Count <= 3);

        var schoon = CareerDreamCatalog.Search("schoon", max: 8);
        Assert.NotEmpty(schoon);
        Assert.Equal("schoonmaker", schoon[0].Key);
        Assert.Equal("Entry", schoon[0].Level);
    }

    [Fact]
    public void Catalog_includes_entry_level_labour_and_school_leaver_roles()
    {
        string[] requiredKeys =
        [
            "orderpicker", "productiemedewerker", "heftruckchauffeur", "schoonmaker",
            "magazijnmedewerker", "zorghulp", "helpende-zorg-en-welzijn", "verzorgende-ig",
            "kok", "bezorger", "chauffeur-c", "chauffeur-ce", "kassamedewerker",
            "tuinder", "kasmedewerker", "elektricien", "loodgieter", "kapper",
            "receptionist", "administratief-medewerker", "software-developer", "leerkracht",
            "verpleegkundige"
        ];

        foreach (var key in requiredKeys)
        {
            Assert.NotNull(CareerDreamCatalog.FindByKey(key));
        }
    }
}
