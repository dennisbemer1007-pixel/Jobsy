using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Services;
using Xunit;

namespace Jobsy.Tests;

public class KandidatenkaartAggregationTests
{
    [Fact]
    public void GridAreaKey_is_stable_for_nearby_points()
    {
        var a = new GeoPoint(52.156, 5.387);
        var b = new GeoPoint(52.158, 5.389);
        Assert.Equal(KandidatenkaartCandidateAggregator.GridAreaKey(a), KandidatenkaartCandidateAggregator.GridAreaKey(b));
    }
}
