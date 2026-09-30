using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class IsochroneServiceTests
{
    [Fact]
    public async Task Mock_isochrone_returns_three_polygons_largest_first()
    {
        IIsochroneService svc = new MockIsochroneService();
        var json = await svc.TryGetIsochronesAsync(52.022, 4.172, TransportMode.Bike, [10, 20, 30]);
        Assert.False(string.IsNullOrWhiteSpace(json));
        using var doc = JsonDocument.Parse(json!);
        var features = doc.RootElement.GetProperty("features");
        Assert.Equal(3, features.GetArrayLength());
        var firstMins = features[0].GetProperty("properties").GetProperty("minutes").GetInt32();
        var lastMins = features[2].GetProperty("properties").GetProperty("minutes").GetInt32();
        Assert.True(firstMins > lastMins);
        Assert.Equal("Polygon", features[0].GetProperty("geometry").GetProperty("type").GetString());
    }

    [Fact]
    public async Task Mock_isochrone_ov_still_returns_circles()
    {
        IIsochroneService svc = new MockIsochroneService();
        var json = await svc.TryGetIsochronesAsync(
            52.022, 4.172, TransportMode.PublicTransport, [10, 20, 30]);
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public void JobMap_fetches_isochrones_and_falls_back_to_circles()
    {
        var root = FindRepoRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("fetchIsochrones", js, StringComparison.Ordinal);
        Assert.Contains("/api/travel/isochrones", js, StringComparison.Ordinal);
        Assert.Contains("isochrone fallback", js, StringComparison.Ordinal);
        Assert.Contains("featuresFromIsochroneFc", js, StringComparison.Ordinal);
        Assert.Contains("buildTravelRingFeatures", js, StringComparison.Ordinal);
        // Graduated fill opacities (banenkaart 03 — stronger rings).
        Assert.Contains("0.22", js, StringComparison.Ordinal);
        Assert.Contains("0.14", js, StringComparison.Ordinal);
        Assert.Contains("0.09", js, StringComparison.Ordinal);
        Assert.Contains("getComputedStyle", js, StringComparison.Ordinal);
        Assert.Contains("--brand", js, StringComparison.Ordinal);
        Assert.Contains("data-iso-mode", js, StringComparison.Ordinal);
        Assert.DoesNotContain("#2563eb", js, StringComparison.Ordinal);
        Assert.DoesNotContain("#1d4ed8", js, StringComparison.Ordinal);

        var proxy = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Hosting", "VacancyMapProxyEndpoints.cs"));
        Assert.Contains("/api/travel/isochrones", proxy, StringComparison.Ordinal);

        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api", "Controllers", "TravelController.cs"));
        Assert.Contains("EnableRateLimiting(\"public-read\")", controller, StringComparison.Ordinal);
        Assert.Contains("IIsochroneService", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Di_registers_mock_isochrones_in_testing()
    {
        var di = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "DependencyInjection.cs"));
        Assert.Contains("IIsochroneService", di, StringComparison.Ordinal);
        Assert.Contains("MockIsochroneService", di, StringComparison.Ordinal);
        Assert.Contains("ValhallaIsochroneService", di, StringComparison.Ordinal);
        Assert.Contains("Routing:IsochroneBaseUrl", di, StringComparison.Ordinal);
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
