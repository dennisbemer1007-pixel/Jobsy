using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.UwvFeatures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// jobMap.min.js must use one ?v= everywhere (asset-versions.json) — stale App.razor / SW tags served acc19 on acc.
/// </summary>
[Collection("BanenkaartPlaywright")]
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
        await using var factory = new BanenkaartJobMapGuardWebFactory();
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

    /// <summary>TestHost only — avoids dual Kestrel <see cref="IllustratedPagesWebFactory"/> startup races under parallel CI.</summary>
    private sealed class BanenkaartJobMapGuardWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["Landing:ForceVariant"] = "on",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                    ["Jobsy:CandidateCompactHeroEnabled"] = "true",
                    ["Features:UwvFeatures"] = "true",
                    ["JobsyFeatures:BedrijvenkaartSsrPreview"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new AlwaysOnUwvFeatureFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new IllustratedPagesStubHttpClientFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new IllustratedPagesStubApiHandler(), disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    }));
            });
        }

        private sealed class AlwaysOnUwvFeatureFlags : IFeatureFlags
        {
            private readonly FeatureFlagSnapshot _snap = new(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                CandidateExternalVacanciesEnabled: true,
                ProactiveMatchingEnabled: false,
                CompanyMapInitiativeEnabled: true,
                UwvFeaturesEnabled: true);

            public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(_snap);

            public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(_snap.IsEnabled(feature));

            public void Invalidate()
            {
            }
        }

        private sealed class IllustratedPagesStubHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
                => new(new IllustratedPagesStubApiHandler(), disposeHandler: true)
                {
                    BaseAddress = new Uri("http://api.test/")
                };
        }

        private sealed class IllustratedPagesStubApiHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                var pathAndQuery = path + (request.RequestUri?.Query ?? "");
                var json = IllustratedPagesApiStub.BuildJson(pathAndQuery);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
