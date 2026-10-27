using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Tests.Scholen;

public sealed class PupilJourneyThemeTests
{
    [Theory]
    [InlineData(PupilJourneyTheme.Zee, "koraalrif", "Leerling.World.Reef.Title")]
    [InlineData(PupilJourneyTheme.Tuin, "koraalrif", "Leerling.Theme.Tuin.Reef.Title")]
    [InlineData(PupilJourneyTheme.Ster, "vuurtoren", "Leerling.Theme.Ster.Lighthouse.Title")]
    public void World_title_keys_follow_theme(string theme, string world, string expected)
    {
        Assert.Equal(expected, PupilJourneyTheme.WorldTitleKey(theme, world));
    }

    [Fact]
    public void Groep78_requires_theme_until_chosen()
    {
        Assert.True(PupilJourneyTheme.RequiresThemeChoice(
            Jobsy.Core.Enums.PupilQuestionSet.Groep78,
            null));
        Assert.False(PupilJourneyTheme.RequiresThemeChoice(
            Jobsy.Core.Enums.PupilQuestionSet.Groep78,
            PupilJourneyTheme.Tuin));
        Assert.False(PupilJourneyTheme.RequiresThemeChoice(
            PupilQuestionSet.Vo,
            null));
    }

    [Fact]
    public void Can_change_theme_before_first_answer_or_on_island_only()
    {
        var def = new PupilQuestionSetRegistry().Get(PupilQuestionSet.Groep78);
        var empty = new Dictionary<string, int>();
        Assert.True(PupilJourneyTheme.CanChangeVisualTheme(
            PupilQuestionSet.Groep78,
            PupilCodeStatus.InProgress,
            null,
            empty,
            islandDone: false,
            def,
            puzzles: null));

        var mid = new Dictionary<string, int> { ["9001"] = 3 };
        Assert.False(PupilJourneyTheme.CanChangeVisualTheme(
            PupilQuestionSet.Groep78,
            PupilCodeStatus.InProgress,
            null,
            mid,
            islandDone: false,
            def,
            puzzles: null));

        var atIsland = def.Bank.AllItems.Take(30).ToDictionary(i => i.Id, _ => 3);
        var puzzlesAtIsland = new Dictionary<string, PupilPuzzleStatus>
        {
            ["schelpenrij"] = PupilPuzzleStatus.Done,
            ["schatkaart"] = PupilPuzzleStatus.Done,
        };
        Assert.True(PupilJourneyTheme.CanChangeVisualTheme(
            PupilQuestionSet.Groep78,
            PupilCodeStatus.InProgress,
            null,
            atIsland,
            islandDone: false,
            def,
            puzzlesAtIsland));
    }

    [Theory]
    [InlineData(PupilJourneyTheme.Tuin, "schelpenrij", "Leerling.Puzzle.Tuin.Schelpenrij.Title")]
    [InlineData(PupilJourneyTheme.Zee, "schelpenrij", "Leerling.Puzzle.Schelpenrij.Title")]
    public void Puzzle_title_keys_follow_theme(string theme, string puzzle, string expected)
    {
        Assert.Equal(expected, PupilJourneyTheme.PuzzleTitleKey(theme, puzzle));
    }
}
