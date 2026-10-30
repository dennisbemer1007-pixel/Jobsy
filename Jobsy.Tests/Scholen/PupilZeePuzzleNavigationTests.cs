using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilZeePuzzleNavigationTests
{
    [Fact]
    public void TargetPath_zee_schelpenrij_goes_to_intro()
    {
        var path = PupilZeePuzzleNavigation.TargetPath(
            PupilQuestionSet.Groep78,
            PupilJourneyTheme.ZeeExpeditie,
            "puzzle",
            "schelpenrij");
        Assert.Equal("/leerling/puzzel-intro/schelpenrij", path);
    }

    [Fact]
    public void TargetPath_classic_theme_goes_to_puzzle_page()
    {
        var path = PupilZeePuzzleNavigation.TargetPath(
            PupilQuestionSet.Groep78,
            PupilJourneyTheme.Eilandtuin,
            "puzzle",
            "schelpenrij");
        Assert.Equal("/leerling/puzzel/schelpenrij", path);
    }

    [Fact]
    public void IntroNodeIndex_schelpenrij_is_second_stop()
    {
        Assert.Equal(1, PupilZeeJourneyBar.IntroNodeIndex("schelpenrij"));
    }
}
