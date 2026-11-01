using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Tests.Scholen;

public class PupilGroep78JourneyTests
{
    private readonly PupilQuestionSetDef _def = new PupilQuestionSetRegistry().Get(PupilQuestionSet.Groep78);

    [Fact]
    public void BuildNodes_includes_worlds_puzzles_island_vakken_and_result()
    {
        var nodes = PupilGroep78Journey.BuildNodes(_def);
        Assert.Contains(nodes, n => n.Key == "koraalrif" && n.Kind == PupilGroep78JourneyNodeKind.World);
        Assert.Contains(nodes, n => n.Key == "schelpenrij" && n.Kind == PupilGroep78JourneyNodeKind.Puzzle);
        Assert.Contains(nodes, n => n.Key == "pauze-eiland" && n.Kind == PupilGroep78JourneyNodeKind.Island);
        Assert.Contains(nodes, n => n.Key == "lagune" && n.Kind == PupilGroep78JourneyNodeKind.Vakken);
        Assert.Contains(nodes, n => n.Key == "dit-ben-jij" && n.Kind == PupilGroep78JourneyNodeKind.Result);
    }

    [Fact]
    public void DotImageStem_result_uses_theme_map()
    {
        var node = new PupilGroep78JourneyNode("dit-ben-jij", PupilGroep78JourneyNodeKind.Result);
        Assert.Equal("zeekaart", PupilGroep78Journey.DotImageStem(PupilJourneyTheme.ZeeExpeditie, node));
        Assert.Equal("tuin-kaart", PupilGroep78Journey.DotImageStem(PupilJourneyTheme.Eilandtuin, node));
        Assert.Equal("sterren-kaart", PupilGroep78Journey.DotImageStem(PupilJourneyTheme.Sterrenkust, node));
    }
}
