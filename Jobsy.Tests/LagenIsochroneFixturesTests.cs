using System.Text.Json;

namespace Jobsy.Tests;

public sealed class LagenIsochroneFixturesTests
{
    [Fact]
    public void Isochrone_fixture_honors_requested_minutes_including_60()
    {
        var json = LagenIsochroneFixtures.FeatureCollectionJson(52.0705, 4.3007, "60");
        using var doc = JsonDocument.Parse(json);
        var features = doc.RootElement.GetProperty("features");
        Assert.Equal(1, features.GetArrayLength());
        Assert.Equal(60, features[0].GetProperty("properties").GetProperty("minutes").GetInt32());
    }
}
