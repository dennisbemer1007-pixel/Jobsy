using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilGroep78EntryPathsTests
{
    [Theory]
    [InlineData("/leerling/reis", true)]
    [InlineData("/leerling/start", true)]
    [InlineData("/leerling/vakken", true)]
    [InlineData("/leerling/eiland", true)]
    [InlineData("/leerling/puzzel/schelpenrij", true)]
    [InlineData("/leerling/wereld", false)]
    [InlineData("/leerling/pdf", false)]
    public void IsThemeGatedPath_matches_g78_journey_steps(string path, bool gated)
        => Assert.Equal(gated, PupilGroep78EntryPaths.IsThemeGatedPath(path));

    [Fact]
    public void RequiresThemeChoice_only_for_groep78_without_explicit_choice()
    {
        Assert.True(PupilGroep78EntryPaths.RequiresThemeChoice(PupilQuestionSet.Groep78, null, null));
        Assert.True(PupilGroep78EntryPaths.RequiresThemeChoice(PupilQuestionSet.Groep78, "", null));
        Assert.True(PupilGroep78EntryPaths.RequiresThemeChoice(
            PupilQuestionSet.Groep78,
            PupilJourneyTheme.ZeeExpeditie,
            null));
        Assert.True(PupilGroep78EntryPaths.RequiresThemeChoice(
            PupilQuestionSet.Groep78,
            PupilJourneyTheme.Sterrenkust,
            null));
        Assert.False(PupilGroep78EntryPaths.RequiresThemeChoice(
            PupilQuestionSet.Groep78,
            PupilJourneyTheme.ZeeExpeditie,
            DateTime.UtcNow));
        Assert.False(PupilGroep78EntryPaths.RequiresThemeChoice(PupilQuestionSet.Vo, null, null));
    }

    [Fact]
    public void AfterLogin_groep78_without_explicit_choice_goes_to_wereld()
    {
        var path = PupilGroep78EntryPaths.AfterLogin(
            PupilQuestionSet.Groep78,
            PupilCodeStatus.NotStarted,
            new Jobsy.Core.Entities.Scholen.PupilProgress
            {
                JourneyThemeKey = PupilJourneyTheme.Sterrenkust,
            },
            completed: false);
        Assert.Equal(PupilGroep78EntryPaths.Wereld, path);
    }
}
