using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Xunit;

namespace Jobsy.Tests;

public sealed class CompanyMapInitiativeFeatureFlagTests
{
    [Fact]
    public void PlatformFeatureSettings_default_on()
    {
        Assert.True(new PlatformFeatureSettings().CompanyMapInitiativeEnabled);
    }

    [Fact]
    public void FeatureFlagSnapshot_default_on_and_requires_employers()
    {
        var d = FeatureFlagSnapshot.Defaults;
        Assert.True(d.CompanyMapInitiativeEnabled);
        Assert.False(d.IsEnabled(PlatformFeature.CompanyMapInitiative));

        var on = d with { EmployersEnabled = true };
        Assert.True(on.IsEnabled(PlatformFeature.CompanyMapInitiative));
    }
}
