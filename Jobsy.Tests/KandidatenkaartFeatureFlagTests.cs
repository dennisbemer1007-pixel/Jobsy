using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Xunit;

namespace Jobsy.Tests;

public class KandidatenkaartFeatureFlagTests
{
    [Fact]
    public void PlatformFeatureSettings_kandidatenkaart_default_off()
    {
        Assert.False(new PlatformFeatureSettings().KandidatenkaartEnabled);
    }

    [Fact]
    public void FeatureFlagSnapshot_kandidatenkaart_default_off()
    {
        var d = FeatureFlagSnapshot.Defaults;
        Assert.False(d.KandidatenkaartEnabled);
        Assert.False(d.IsEnabled(PlatformFeature.Kandidatenkaart));
    }
}
