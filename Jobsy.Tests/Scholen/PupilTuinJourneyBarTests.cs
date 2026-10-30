using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilTuinJourneyBarTests
{
    [Theory]
    [InlineData("koraalrif", 0)]
    [InlineData("schatgrot", 2)]
    [InlineData("vuurtoren", 5)]
    [InlineData("lagune", 7)]
    public void CurrentNodeIndex_maps_world_segments(string worldKey, int expected)
        => Assert.Equal(expected, PupilTuinJourneyBar.CurrentNodeIndex(worldKey, completed: false));

    [Fact]
    public void IntroNodeIndex_schelpenrij_is_second_stop()
        => Assert.Equal(1, PupilTuinJourneyBar.IntroNodeIndex("schelpenrij"));
}
