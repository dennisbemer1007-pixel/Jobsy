using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilSterJourneyBarTests
{
    [Theory]
    [InlineData("koraalrif", 0)]
    [InlineData("schatgrot", 2)]
    [InlineData("vuurtoren", 5)]
    [InlineData("lagune", 7)]
    public void CurrentNodeIndex_maps_world(string worldKey, int expected)
        => Assert.Equal(expected, PupilSterJourneyBar.CurrentNodeIndex(worldKey, completed: false));

    [Fact]
    public void IntroNodeIndex_schelpenrij_is_first_puzzle_stop()
        => Assert.Equal(1, PupilSterJourneyBar.IntroNodeIndex("schelpenrij"));
}
