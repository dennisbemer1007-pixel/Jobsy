using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests;

public class ValhallaIsochroneParsingTests
{
    private const string ValhallaShapedDecimal = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "contour": 10.0, "metric": "time" },
              "geometry": { "type": "Polygon", "coordinates": [[[4.2,52.0],[4.3,52.0],[4.3,52.1],[4.2,52.1],[4.2,52.0]]] }
            },
            {
              "type": "Feature",
              "properties": { "contour": 20.0, "metric": "time" },
              "geometry": { "type": "Polygon", "coordinates": [[[4.1,51.9],[4.4,51.9],[4.4,52.2],[4.1,52.2],[4.1,51.9]]] }
            },
            {
              "type": "Feature",
              "properties": { "contour": 30.0, "metric": "time" },
              "geometry": { "type": "Polygon", "coordinates": [[[4.0,51.8],[4.5,51.8],[4.5,52.3],[4.0,52.3],[4.0,51.8]]] }
            }
          ]
        }
        """;

    [Fact]
    public void Valhalla_decimal_contours_normalise_to_minutes_10_20_30()
    {
        using var doc = JsonDocument.Parse(ValhallaShapedDecimal);
        var normalized = ValhallaIsochroneService.NormalizeFeatureCollection(doc.RootElement, [30, 20, 10]);
        Assert.False(string.IsNullOrWhiteSpace(normalized));
        using var outDoc = JsonDocument.Parse(normalized!);
        var features = outDoc.RootElement.GetProperty("features");
        Assert.Equal(3, features.GetArrayLength());
        Assert.Equal(30, features[0].GetProperty("properties").GetProperty("minutes").GetInt32());
        Assert.Equal(20, features[1].GetProperty("properties").GetProperty("minutes").GetInt32());
        Assert.Equal(10, features[2].GetProperty("properties").GetProperty("minutes").GetInt32());
    }

    [Fact]
    public void Integer_contour_still_works()
    {
        using var doc = JsonDocument.Parse("""
            {"type":"FeatureCollection","features":[
              {"type":"Feature","properties":{"contour":20},"geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]}}
            ]}
            """);
        var normalized = ValhallaIsochroneService.NormalizeFeatureCollection(doc.RootElement, [20]);
        Assert.NotNull(normalized);
        using var outDoc = JsonDocument.Parse(normalized!);
        Assert.Equal(20, outDoc.RootElement.GetProperty("features")[0]
            .GetProperty("properties").GetProperty("minutes").GetInt32());
    }

    [Theory]
    [InlineData("""{"contour":19.6}""", 20)]
    [InlineData("""{"contour":10.4}""", 10)]
    [InlineData("""{"minutes":15.5}""", 16)]
    [InlineData("""{"time":30}""", 30)]
    public void ReadMinutes_rounds_decimals_away_from_zero(string propsJson, int expected)
    {
        using var doc = JsonDocument.Parse(propsJson);
        Assert.Equal(expected, ValhallaIsochroneService.ReadMinutes(doc.RootElement));
    }

    [Theory]
    [InlineData("""{"contour":0.0}""")]
    [InlineData("""{"contour":-5}""")]
    [InlineData("""{"contour":241}""")]
    [InlineData("""{"contour":"NaN"}""")]
    [InlineData("""{"contour":null}""")]
    [InlineData("""{}""")]
    public void ReadMinutes_skips_invalid(string propsJson)
    {
        using var doc = JsonDocument.Parse(propsJson);
        Assert.Null(ValhallaIsochroneService.ReadMinutes(doc.RootElement));
    }

    [Fact]
    public async Task TravelController_returns_200_for_normalised_valhalla_payload()
    {
        var geojson = ValhallaIsochroneService.NormalizeFeatureCollection(
            JsonDocument.Parse(ValhallaShapedDecimal).RootElement,
            [30, 20, 10]);
        Assert.NotNull(geojson);

        IIsochroneService fake = new StubIsochrone(geojson!);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var controller = new TravelController(fake, cache)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetIsochrones(52.0205, 4.2476, mode: "Fiets", minutes: "10,20,30");
        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode ?? 200);
        Assert.Contains("application/geo+json", content.ContentType ?? "", StringComparison.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(content.Content!);
        Assert.Equal(3, doc.RootElement.GetProperty("features").GetArrayLength());
    }

    private sealed class StubIsochrone(string json) : IIsochroneService
    {
        public Task<string?> TryGetIsochronesAsync(
            double latitude,
            double longitude,
            TransportMode mode,
            IReadOnlyList<int> minutes,
            CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(json);
    }
}
