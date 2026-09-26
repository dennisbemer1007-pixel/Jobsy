using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;

namespace Jobsy.Tests;

/// <summary>
/// Compact map pins endpoint — id/lat/lng/colour/match% only, with ETag caching.
/// </summary>
public class VacancyPinsApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public VacancyPinsApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Pins_returns_compact_fields_only()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/vacancies/pins");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        var pins = JsonSerializer.Deserialize<List<VacancyPinDto>>(raw, JsonOpts);
        Assert.NotNull(pins);
        Assert.NotEmpty(pins!);

        var first = pins[0];
        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.True(double.IsFinite(first.Lat));
        Assert.True(double.IsFinite(first.Lng));

        Assert.DoesNotContain("\"title\"", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("companyName", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("description", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("companyAddress", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pins_supports_etag_304()
    {
        var client = _factory.CreateClient();
        var first = await client.GetAsync("api/vacancies/pins");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.True(first.Headers.ETag is not null);
        Assert.Contains("max-age=", first.Headers.CacheControl?.ToString() ?? "", StringComparison.Ordinal);

        var etag = first.Headers.ETag!.Tag;
        var request = new HttpRequestMessage(HttpMethod.Get, "api/vacancies/pins");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var second = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public void Discovery_uses_compact_markers_and_pins_url()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("BuildCompactPins", discovery);
        Assert.Contains("BuildPinsApiUrl", discovery);
        Assert.Contains("pinsUrl", discovery);
        Assert.Contains("VacancyCardPageSize = 20", discovery);
        Assert.Contains("jobsyList.observeMore", discovery);
        Assert.DoesNotContain("[\"title\"] = v.Title", discovery);
        Assert.DoesNotContain("[\"company\"] = v.CompanyName", discovery);

        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("function fetchPins", js);
        Assert.Contains("function fetchVacancyDetail", js);
        Assert.Contains("function normalizePin", js);
        Assert.Contains("reloadPins", js);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("/login", maps);
        Assert.Contains("Never pull MapLibre", maps);
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
