using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class EncodedPolylineTests
{
    [Fact]
    public void Decode_wikipedia_example()
    {
        var points = EncodedPolyline.Decode("_p~iF~ps|U_ulLnnqC_mqNvxq`@", precision: 5);

        Assert.Equal(3, points.Count);
        Assert.Equal(38.5, points[0].Latitude, 3);
        Assert.Equal(-120.2, points[0].Longitude, 3);
        Assert.Equal(40.7, points[1].Latitude, 3);
        Assert.Equal(-120.95, points[1].Longitude, 3);
        Assert.Equal(43.252, points[2].Latitude, 3);
        Assert.Equal(-126.453, points[2].Longitude, 3);
    }

    [Fact]
    public void LengthMeters_is_positive_for_a_path()
    {
        var meters = EncodedPolyline.LengthMeters("_p~iF~ps|U_ulLnnqC_mqNvxq`@", precision: 5);
        Assert.True(meters > 100_000);
    }
}

public class OsrmRoutingServiceTests
{
    [Fact]
    public async Task Bike_uses_osrm_cycling_and_parses_route()
    {
        HttpRequestMessage? captured = null;
        var sut = CreateSut(request =>
        {
            captured = request;
            return OsrmOk(6637.5, 709.8);
        });

        var route = await sut.TryGetRouteAsync(52.0705, 4.3007, 52.1133, 4.2812, TransportMode.Bike);

        Assert.NotNull(route);
        Assert.Equal(6637.5, route!.DistanceMeters);
        Assert.Equal(709.8, route.DurationSeconds);
        Assert.Equal(TransportMode.Bike, route.TransportMode);
        Assert.Contains("/route/v1/cycling/", captured!.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain("52.0705,4.3007", captured.RequestUri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains("4.3007,52.0705", captured.RequestUri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Car_and_walk_use_matching_osrm_profiles()
    {
        var profiles = new List<string>();
        var sut = CreateSut(request =>
        {
            profiles.Add(request.RequestUri!.AbsolutePath);
            return OsrmOk(1000, 120);
        });

        await sut.TryGetRouteAsync(52.1, 4.31, 52.2, 4.32, TransportMode.Car);
        await sut.TryGetRouteAsync(52.11, 4.33, 52.21, 4.34, TransportMode.Walking);

        Assert.Contains(profiles, p => p.Contains("/driving/", StringComparison.Ordinal));
        Assert.Contains(profiles, p => p.Contains("/walking/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Transit_reads_motis_duration_and_leg_geometry_distance()
    {
        var sut = CreateSut(request =>
        {
            Assert.Contains("/api/v5/plan", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            Assert.Contains("fromPlace=", request.RequestUri.AbsoluteUri, StringComparison.Ordinal);
            return Json(HttpStatusCode.OK, """
                {
                  "itineraries": [
                    {
                      "duration": 2100,
                      "legs": [
                        { "mode": "WALK", "distance": 699.0, "duration": 720 },
                        {
                          "mode": "TRAM",
                          "duration": 300,
                          "legGeometry": { "points": "_p~iF~ps|U_ulLnnqC_mqNvxq`@", "precision": 5 }
                        }
                      ]
                    }
                  ]
                }
                """);
        });

        var route = await sut.TryGetRouteAsync(52.08, 4.29, 52.12, 4.27, TransportMode.PublicTransport);

        Assert.NotNull(route);
        Assert.Equal(2100, route!.DurationSeconds);
        Assert.True(route.DistanceMeters > 100_000);
        Assert.Equal(TransportMode.PublicTransport, route.TransportMode);
    }

    [Fact]
    public async Task Osrm_error_returns_null()
    {
        var sut = CreateSut(_ => Json(HttpStatusCode.BadRequest, """{"code":"InvalidQuery"}"""));
        var route = await sut.TryGetRouteAsync(51.0, 4.0, 51.1, 4.1, TransportMode.Bike);
        Assert.Null(route);
    }

    [Fact]
    public void Vacancy_travel_endpoint_is_anonymous_and_uses_exact_router()
    {
        var controller = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api", "Controllers", "VacanciesController.cs"));
        Assert.Contains("[HttpGet(\"{id:guid}/travel\")]", controller);
        Assert.Contains("IExactRoutingService", controller);
        Assert.Contains("TryExactRouteAsync", controller);
        Assert.DoesNotContain("_routing.GetRouteAsync", controller);
    }

    private static OsrmRoutingService CreateSut(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Routing:OsrmBaseUrl"] = "https://router.project-osrm.org",
                ["Routing:TransitBaseUrl"] = "https://api.transitous.org",
                ["Routing:TransitPlanPath"] = "/api/v5/plan"
            })
            .Build();

        return new OsrmRoutingService(
            new StubFactory(new StubHandler(responder)),
            config,
            NullLogger<OsrmRoutingService>.Instance);
    }

    private static HttpResponseMessage OsrmOk(double meters, double seconds)
        => Json(HttpStatusCode.OK, $$"""{"code":"Ok","routes":[{"distance":{{meters.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"duration":{{seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}]}""");

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

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

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
