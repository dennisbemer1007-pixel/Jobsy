using Jobsy.Core.Entities;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class KreeftJourneyZoneTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 2)]
    [InlineData(8, 3)]
    [InlineData(10, 3)]
    public void Onboarding_step_maps_to_expected_zone(int step, int expectedZone)
    {
        Assert.Equal(expectedZone, KreeftJourneyZone.ZoneIndexForOnboardingStep(step));
    }

    [Fact]
    public void Missing_onboarding_resolves_to_strand_zone()
    {
        Assert.Equal(0, KreeftJourneyZone.ResolveZoneIndex(null));
        Assert.Equal("Strand", KreeftJourneyZone.ZoneLabel(0, "nl"));
    }

    [Fact]
    public void Finished_onboarding_resolves_to_licht_zone()
    {
        var row = new CandidateOnboarding
        {
            CurrentStep = 10,
            FinishReached = true,
            StepsJson = "[]"
        };
        Assert.Equal(4, KreeftJourneyZone.ResolveZoneIndex(row));
        Assert.Equal("Licht", KreeftJourneyZone.ZoneLabel(4, "nl"));
    }
}
