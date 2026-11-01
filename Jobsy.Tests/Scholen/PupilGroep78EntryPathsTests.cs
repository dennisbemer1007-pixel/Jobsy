using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilGroep78EntryPathsTests
{
    [Theory]
    [InlineData("/leerling/reis", true)]
    [InlineData("/leerling/vakken", true)]
    [InlineData("/leerling/eiland", true)]
    [InlineData("/leerling/puzzel/schelpenrij", true)]
    [InlineData("/leerling/wereld", false)]
    [InlineData("/leerling/start", false)]
    [InlineData("/leerling/pdf", false)]
    public void IsThemeGatedPath_matches_g78_journey_steps(string path, bool gated)
        => Assert.Equal(gated, PupilGroep78EntryPaths.IsThemeGatedPath(path));

    [Fact]
    public void RequiresThemeChoice_only_for_groep78_without_valid_key()
    {
        Assert.True(PupilGroep78EntryPaths.RequiresThemeChoice(PupilQuestionSet.Groep78, null));
        Assert.True(PupilGroep78EntryPaths.RequiresThemeChoice(PupilQuestionSet.Groep78, ""));
        Assert.False(PupilGroep78EntryPaths.RequiresThemeChoice(PupilQuestionSet.Groep78, PupilJourneyTheme.ZeeExpeditie));
        Assert.False(PupilGroep78EntryPaths.RequiresThemeChoice(PupilQuestionSet.Vo, null));
    }
}
