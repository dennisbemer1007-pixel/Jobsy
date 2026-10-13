using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart card/pin contracts + same-origin web proxy regression guard.
/// </summary>
public class VacancyMapProxyAndCardTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public VacancyMapProxyAndCardTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Pins_include_marker_fields_for_featured_and_work_type()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/vacancies/pins");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        var pins = JsonSerializer.Deserialize<List<VacancyPinDto>>(raw, JsonOpts);
        Assert.NotNull(pins);
        Assert.NotEmpty(pins!);

        Assert.Contains("highlighted", raw, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("highlightRank", raw, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workType", raw, StringComparison.OrdinalIgnoreCase);

        Assert.NotEqual(Guid.Empty, pins[0].Id);
        Assert.DoesNotContain("\"title\"", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("companyName", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Card_and_batch_endpoints_return_popup_fields_from_index()
    {
        var client = _factory.CreateClient();
        var pins = await client.GetFromJsonAsync<List<VacancyPinDto>>("api/vacancies/pins", JsonOpts);
        Assert.NotNull(pins);
        Assert.NotEmpty(pins!);
        var id = pins[0].Id;

        var cardResponse = await client.GetAsync($"api/vacancies/{id:D}/card");
        Assert.Equal(HttpStatusCode.OK, cardResponse.StatusCode);
        var card = await cardResponse.Content.ReadFromJsonAsync<VacancyCardDto>(JsonOpts);
        Assert.NotNull(card);
        Assert.Equal(id, card!.Id);
        Assert.False(string.IsNullOrWhiteSpace(card.Title));
        Assert.False(string.IsNullOrWhiteSpace(card.CompanyName));
        Assert.False(string.IsNullOrWhiteSpace(card.ThumbnailUrl));

        var batch = await client.GetFromJsonAsync<List<VacancyCardDto>>(
            $"api/vacancies/cards?ids={id:D}",
            JsonOpts);
        Assert.NotNull(batch);
        Assert.Single(batch!);
        Assert.Equal(card.Title, batch[0].Title);
    }

    [Fact]
    public void JobMap_calls_card_and_pins_urls_with_cluster_render()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("/api/vacancies/", js);
        Assert.Contains("/card", js);
        Assert.Contains("/api/vacancies/cards?ids=", js);
        Assert.Contains("function fetchVacancyCard", js);
        Assert.Contains("function renderClusterPage", js);
        Assert.Contains("map-popup__main", js);
        Assert.Contains("data-retry-card", js);
        Assert.Contains("function reloadPins", js);
        Assert.Contains("clusterRadius", js);
        Assert.Contains("jobsy-pins", js);
        Assert.Contains("getClusterLeaves", js);
        Assert.Contains("openClusterList", js);
        Assert.Contains("async function onClusterClick", js);
        Assert.Contains("setFeatureState", js);
        Assert.Contains("AbortController", js);
        Assert.DoesNotContain(
            "getClusterExpansionZoom(clusterId, function",
            js,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "getClusterLeaves(clusterId, 100, 0, function",
            js,
            StringComparison.Ordinal);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        AssetVersions.AssertVersionedRefMatchesManifest(maps, "js/jobMap.min.js");
        Assert.Contains("photoIsWorkTypePlaceholder", js);
        Assert.Contains("/api/vacancies/{id:guid}/image",
            File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "VacancyMapProxyEndpoints.cs")));
        var discovery = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        // Full vacancy catalog must never be persisted into / HTML (2 MB hub limit).
        Assert.DoesNotContain("PersistentComponentState", discovery);
        Assert.DoesNotContain("PersistAsJson", discovery);
        Assert.DoesNotContain("DiscoveryPersistState", discovery);
        Assert.Contains("await source.getClusterLeaves", js, StringComparison.Ordinal);
        var onClusterStart = js.IndexOf("async function onClusterClick", StringComparison.Ordinal);
        Assert.True(onClusterStart >= 0);
        var onClusterEnd = js.IndexOf("\n    function onPinClick", onClusterStart, StringComparison.Ordinal);
        Assert.True(onClusterEnd > onClusterStart);
        var onCluster = js.Substring(onClusterStart, onClusterEnd - onClusterStart);
        Assert.Contains("openClusterList", onCluster, StringComparison.Ordinal);
        Assert.DoesNotContain("easeTo", onCluster, StringComparison.Ordinal);
        Assert.Contains("fetchJsonWithOneRetry", js);
        Assert.Contains("retryAfterMs", js);
        // Cluster paging uses pin/card HTML in a fixed frame — never the shimmer skeleton.
        var renderClusterStart = js.IndexOf("function renderClusterPage", StringComparison.Ordinal);
        Assert.True(renderClusterStart >= 0);
        var renderClusterEnd = js.IndexOf("\n    function bindClusterPopupInteractions", renderClusterStart, StringComparison.Ordinal);
        Assert.True(renderClusterEnd > renderClusterStart);
        var renderCluster = js.Substring(renderClusterStart, renderClusterEnd - renderClusterStart);
        Assert.DoesNotContain("skeletonPopupHtml", renderCluster, StringComparison.Ordinal);
        Assert.DoesNotContain("map-popup--skeleton", renderCluster, StringComparison.Ordinal);
        Assert.Contains("prefetchClusterCards", js, StringComparison.Ordinal);
        Assert.Contains("map-cluster-card", js, StringComparison.Ordinal);
        // Single-pin flow may still use the skeleton helper.
        Assert.Contains("function skeletonPopupHtml", js, StringComparison.Ordinal);
        Assert.Contains("touchstart", js);
    }

    [Fact]
    public void Proxy_forwards_visitor_ip_and_caches_anonymous_reads()
    {
        var proxy = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "VacancyMapProxyEndpoints.cs"));
        Assert.Contains("ApplyVisitorIdentity", proxy);
        Assert.Contains("TrustedClientIp.Resolve", proxy);
        Assert.Contains("Retry-After", proxy);
        Assert.Contains("AnonymousCacheTtl", proxy);
        Assert.Contains("InternalClientIpHeaders.ClientIpHeader", proxy);
        Assert.Contains("InternalClientIpHeaders.InternalSecretHeader", proxy);
        Assert.Contains("Never cache failures", proxy);

        var auth = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Services", "JobsyApiAuthHandler.cs"));
        Assert.Contains("TrustedClientIpHandler.ApplyTrustedClientIp", auth);
        Assert.Contains("TrustedClientIp.Resolve", File.ReadAllText(
            Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "VacancyMapProxyEndpoints.cs")));
    }

    [Fact]
    public void ResolveVisitorIp_uses_remote_ip_not_raw_cf_header()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["CF-Connecting-IP"] = "203.0.113.44";
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        Assert.Equal("10.0.0.1", VacancyMapApiForwarder.ResolveVisitorIp(http));
    }

    [Fact]
    public async Task Web_app_proxies_every_jobMap_api_url_with_200()
    {
        var apiClient = _factory.CreateClient();
        var pins = await apiClient.GetFromJsonAsync<List<VacancyPinDto>>("api/vacancies/pins", JsonOpts);
        Assert.NotNull(pins);
        Assert.NotEmpty(pins!);
        var sampleId = pins[0].Id;

        await using var web = new VacancyMapWebProxyFactory(_factory);
        var webClient = web.CreateClient();

        var urls = new[]
        {
            "/api/vacancies/pins",
            $"/api/vacancies/{sampleId:D}/card",
            $"/api/vacancies/cards?ids={sampleId:D}",
            $"/api/vacancies/{sampleId:D}"
        };

        foreach (var url in urls)
        {
            var response = await webClient.GetAsync(url);
            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotModified,
                $"{url} → {response.StatusCode}");
        }
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

/// <summary>
/// Jobsy.Web TestServer that forwards map proxy calls to the API test factory.
/// </summary>
public sealed class VacancyMapWebProxyFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    private readonly RoleFunctionalWebAppFactory _api;

    public VacancyMapWebProxyFactory(RoleFunctionalWebAppFactory api) => _api = api;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(new TestApiForwarder(_api));
        });
    }

    private sealed class TestApiForwarder : IVacancyMapApiForwarder
    {
        private readonly RoleFunctionalWebAppFactory _api;

        public TestApiForwarder(RoleFunctionalWebAppFactory api) => _api = api;

        public async Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
        {
            var client = _api.CreateClient();
            var target = apiPath;
            if (http.Request.QueryString.HasValue)
            {
                target += http.Request.QueryString.Value;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, target);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (http.Request.Headers.TryGetValue("If-None-Match", out var etag))
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", etag.ToString());
            }

            using var response = await client.SendAsync(request, ct);
            http.Response.StatusCode = (int)response.StatusCode;
            if (response.Headers.ETag is { } responseEtag)
            {
                http.Response.Headers.ETag = responseEtag.ToString();
            }

            var cache = response.Headers.CacheControl?.ToString();
            if (!string.IsNullOrWhiteSpace(cache))
            {
                http.Response.Headers.CacheControl = cache;
            }

            await response.Content.CopyToAsync(http.Response.Body, ct);
        }
    }
}
