using Jobsy.Web.Seo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Jobsy.Tests;

public class LegacyMapQueryTests
{
    [Theory]
    [InlineData("company")]
    [InlineData("companies")]
    [InlineData("workType")]
    [InlineData("q")]
    [InlineData("maxMinutes")]
    [InlineData("transport")]
    [InlineData("minHours")]
    [InlineData("maxHours")]
    public void Known_map_keys_are_deep_links(string key)
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            [key] = "1"
        });
        Assert.True(LegacyMapQuery.IsMapDeepLink(query));
    }

    [Fact]
    public void Unrelated_query_is_not_a_map_deep_link()
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["lang"] = "en",
            ["utm_source"] = "x"
        });
        Assert.False(LegacyMapQuery.IsMapDeepLink(query));
        Assert.False(LegacyMapQuery.IsMapDeepLink(QueryCollection.Empty));
    }

    [Fact]
    public void Key_list_matches_VacancyDiscovery_TryGetValue_keys()
    {
        var discovery = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web",
            "Components",
            "VacancyDiscovery.razor"));

        var discovered = new HashSet<string>(StringComparer.Ordinal);
        const string marker = "TryGetValue(\"";
        var idx = 0;
        while ((idx = discovery.IndexOf(marker, idx, StringComparison.Ordinal)) >= 0)
        {
            var start = idx + marker.Length;
            var end = discovery.IndexOf('"', start);
            Assert.True(end > start);
            discovered.Add(discovery[start..end]);
            idx = end + 1;
        }

        Assert.NotEmpty(discovered);
        Assert.Equal(
            discovered.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            LegacyMapQuery.MapQueryKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
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
