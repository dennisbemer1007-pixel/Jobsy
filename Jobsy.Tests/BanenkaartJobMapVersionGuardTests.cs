using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobsy.Tests;

/// <summary>
/// jobMap.min.js must use one ?v= everywhere (asset-versions.json) — stale App.razor / SW tags served acc19 on acc.
/// </summary>
public sealed class BanenkaartJobMapVersionGuardTests
{
    private static readonly Regex JobMapVersionRef = new(
        @"jobMap\.min\.js\?v=([^""'\s&<>]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void App_razor_maps_boot_tag_matches_manifest_and_jobMap_version()
    {
        var root = TestRepo.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains(
            "WwwrootAssetCatalog.Url(\"js/banenkaart-maps-boot.js\")",
            app,
            StringComparison.Ordinal);
        Assert.Equal(
            AssetVersions.Version("js/jobMap.min.js"),
            AssetVersions.Version("js/banenkaart-maps-boot.js"));
        Assert.Equal(
            AssetVersions.Version("js/banenkaart-maps-boot.js"),
            Jobsy.Web.Hosting.WwwrootAssetCatalog.Version("js/banenkaart-maps-boot.js"));
    }

    [Fact]
    public void All_host_files_reference_single_jobMap_min_version_from_manifest()
    {
        var root = TestRepo.FindRoot();
        var expected = AssetVersions.Version("js/jobMap.min.js");
        var hostFiles = new[]
        {
            Path.Combine(root, "Jobsy.Web", "Components", "App.razor"),
            Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "app-core.js"),
            Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-maps-boot.js"),
        };

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in hostFiles)
        {
            var text = File.ReadAllText(file);
            foreach (Match m in JobMapVersionRef.Matches(text))
            {
                seen.Add(m.Groups[1].Value);
            }
        }

        Assert.Single(seen);
        Assert.Equal(expected, seen.First());
        Assert.DoesNotContain("acc19", string.Join(' ', seen), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Banenkaart_html_and_boot_scripts_reference_only_manifest_jobMap_version()
    {
        var expected = AssetVersions.Version("js/jobMap.min.js");
        await using var factory = new IllustratedPagesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
        });

        var html = await client.GetStringAsync(
            "/banenkaart?maxMinutes=60&originLat=51.99389784&originLng=4.37897242&originLabel=Delft");

        var tags = JobMapVersionRef.Matches(html).Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.Empty(tags);

        var mapsBootVersion = AssetVersions.Version("js/banenkaart-maps-boot.js");
        Assert.Contains(
            $"/js/banenkaart-maps-boot.js?v={mapsBootVersion}",
            html,
            StringComparison.Ordinal);

        var bootJs = await client.GetStringAsync($"/js/banenkaart-maps-boot.js?v={mapsBootVersion}");
        var bootTags = JobMapVersionRef.Matches(bootJs).Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.Single(bootTags);
        Assert.Equal(expected, bootTags[0]);
    }
}
