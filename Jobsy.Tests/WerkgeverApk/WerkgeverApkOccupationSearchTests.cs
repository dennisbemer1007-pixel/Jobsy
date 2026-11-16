using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkOccupationSearchTests
{
    [Fact]
    public void ForManager_dedupes_and_caps_suggestions_for_orderp()
    {
        var hits = WerkgeverApkOccupationSearch.ForManager("orderp");
        Assert.True(hits.Count is >= 1 and <= 6);
        Assert.Equal(hits.Count, hits.Select(h => h.EscoOccupationId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(hits.Count, hits.Select(h => h.Name.ToLowerInvariant()).Distinct().Count());
        Assert.DoesNotContain(hits, h => h.Name.Contains("bagagemedewerker", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hits, h => h.Name.Contains("orderpicker", StringComparison.OrdinalIgnoreCase));
    }
}
