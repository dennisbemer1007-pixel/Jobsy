using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Xunit;

namespace Jobsy.Tests;

/// <summary>Per-flag smoke: primary gate reads the expected snapshot field.</summary>
public sealed class FeatureFlagBehaviorTests
{
    [Theory]
    [InlineData(PlatformFeature.Kandidatenkaart)]
    [InlineData(PlatformFeature.SensitiveExperience)]
    [InlineData(PlatformFeature.TrainingPartners)]
    [InlineData(PlatformFeature.ReferenceVerification)]
    [InlineData(PlatformFeature.Employers)]
    [InlineData(PlatformFeature.UwvFeatures)]
    public void Snapshot_is_enabled_maps_platform_feature(PlatformFeature feature)
    {
        var snap = new FeatureFlagSnapshot(
            EmployersEnabled: feature == PlatformFeature.Employers,
            CandidatePassportEnabled: true,
            KandidatenkaartEnabled: feature == PlatformFeature.Kandidatenkaart,
            SensitiveExperienceEnabled: feature == PlatformFeature.SensitiveExperience,
            TrainingPartnersEnabled: feature == PlatformFeature.TrainingPartners,
            ReferenceVerificationEnabled: feature == PlatformFeature.ReferenceVerification,
            UwvFeaturesEnabled: feature == PlatformFeature.UwvFeatures);
        Assert.True(snap.IsEnabled(feature));
    }

    [Fact]
    public void Employer_phase2_requires_employers_master_switch()
    {
        var off = new FeatureFlagSnapshot(true, true, EmployerPhase2Enabled: true);
        var on = new FeatureFlagSnapshot(false, true, EmployerPhase2Enabled: true);
        Assert.True(off.IsEnabled(PlatformFeature.EmployerPhase2));
        Assert.False(on.IsEnabled(PlatformFeature.EmployerPhase2));
    }

    [Fact]
    public void Web_feature_flags_maps_public_endpoint_fields()
    {
        var type = typeof(WebFeatureFlags).GetNestedType("FeatureFlagsResponse", System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(type);
        foreach (var def in FeatureFlagRegistry.PublicEndpointFlags)
        {
            var camel = char.ToLowerInvariant(def.Key[0]) + def.Key[1..];
            var prop = type!.GetProperty(camel, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            Assert.True(prop is not null, $"WebFeatureFlags response missing {camel}");
        }
    }
}
