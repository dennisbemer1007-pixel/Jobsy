using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Tests.Scholen;

public class PupilFlowTests
{
    private readonly PupilQuestionSetDef _g78 = new PupilQuestionSetRegistry().Get(Jobsy.Core.Enums.PupilQuestionSet.Groep78);

    [Fact]
    public void Empty_answers_yield_question_0()
    {
        var step = PupilFlow.Next(_g78, new Dictionary<string, int>(), islandDone: false);
        Assert.Equal(PupilFlowStepKind.Question, step.Kind);
        Assert.Equal(0, step.Index);
        Assert.Equal("9001", step.ItemId);
        Assert.Equal("koraalrif", step.WorldKey);
    }

    [Fact]
    public void Twenty_nine_answered_yields_question_29()
    {
        var answers = Fill(29);
        var step = PupilFlow.Next(_g78, answers, islandDone: false);
        Assert.Equal(PupilFlowStepKind.Question, step.Kind);
        Assert.Equal(29, step.Index);
        Assert.Equal("9030", step.ItemId);
    }

    [Fact]
    public void Thirty_answered_without_island_yields_island()
    {
        var answers = Fill(30);
        var step = PupilFlow.Next(_g78, answers, islandDone: false);
        Assert.Equal(PupilFlowStepKind.Island, step.Kind);
        Assert.Equal(30, step.Index);
        Assert.Equal("pauze-eiland", step.WorldKey);
    }

    [Fact]
    public void Island_done_yields_question_30()
    {
        var answers = Fill(30);
        var step = PupilFlow.Next(_g78, answers, islandDone: true);
        Assert.Equal(PupilFlowStepKind.Question, step.Kind);
        Assert.Equal(30, step.Index);
        Assert.Equal("9031", step.ItemId);
        Assert.Equal("vuurtoren", step.WorldKey);
    }

    [Fact]
    public void Sixty_answered_yields_done()
    {
        var answers = Fill(60);
        var step = PupilFlow.Next(_g78, answers, islandDone: true);
        Assert.Equal(PupilFlowStepKind.Done, step.Kind);
        Assert.Equal(60, step.Index);
    }

    [Fact]
    public void Puzzle_slot_at_gate_then_cleared_and_skipped_when_past()
    {
        var bank = new PupilQuestionBank();
        var def = _g78 with
        {
            PuzzleSlots = [new PupilPuzzleSlot(15, "schelpenrij")]
        };

        var atGate = Fill(15);
        var puzzle = PupilFlow.Next(def, atGate, islandDone: false);
        Assert.Equal(PupilFlowStepKind.Puzzle, puzzle.Kind);
        Assert.Equal("schelpenrij", puzzle.PuzzleKey);
        Assert.Equal(15, puzzle.Index);

        var afterStatus = PupilFlow.Next(
            def,
            atGate,
            islandDone: false,
            puzzles: new Dictionary<string, PupilPuzzleStatus>
            {
                ["schelpenrij"] = PupilPuzzleStatus.Done
            });
        Assert.Equal(PupilFlowStepKind.Question, afterStatus.Kind);
        Assert.Equal(15, afterStatus.Index);
        Assert.Equal("9016", afterStatus.ItemId);

        // K7: already past the gate (20 answered) → no puzzle.
        var past = Fill(20);
        var noPuzzle = PupilFlow.Next(def, past, islandDone: false);
        Assert.Equal(PupilFlowStepKind.Question, noPuzzle.Kind);
        Assert.Equal(20, noPuzzle.Index);
        Assert.Null(noPuzzle.PuzzleKey);
        _ = bank;
    }

    [Fact]
    public void Vo_fifty_answered_without_island_yields_island_not_at_thirty()
    {
        var vo = new PupilQuestionSetRegistry().Get(Jobsy.Core.Enums.PupilQuestionSet.Vo);
        var thirty = Fill(vo, 30);
        var stillQuestion = PupilFlow.Next(vo, thirty, islandDone: false);
        Assert.Equal(PupilFlowStepKind.Question, stillQuestion.Kind);
        Assert.Equal(30, stillQuestion.Index);

        var fifty = Fill(vo, 50);
        var island = PupilFlow.Next(vo, fifty, islandDone: false);
        Assert.Equal(PupilFlowStepKind.Island, island.Kind);
        Assert.Equal(50, island.Index);

        var after = PupilFlow.Next(vo, fifty, islandDone: true);
        Assert.Equal(PupilFlowStepKind.Question, after.Kind);
        Assert.Equal(50, after.Index);
        Assert.Equal("9151", after.ItemId);
    }

    [Fact]
    public void Vo_reconnect_before_chips_stays_on_the_island_likes_are_optional()
    {
        var vo = new PupilQuestionSetRegistry().Get(Jobsy.Core.Enums.PupilQuestionSet.Vo);
        var fifty = Fill(vo, 50);

        var dropped = PupilFlow.Next(vo, fifty, islandDone: false);
        Assert.Equal(PupilFlowStepKind.Island, dropped.Kind);

        var savedEmpty = PupilFlow.Next(vo, fifty, islandDone: true);
        Assert.Equal(PupilFlowStepKind.Question, savedEmpty.Kind);
        Assert.Equal("9151", savedEmpty.ItemId);

        var help = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/Help/PageHelpDocs.cs"));
        Assert.Contains("Klaar, verder! mag ook leeg.", help, StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private Dictionary<string, int> Fill(int count) => Fill(_g78, count);

    private static Dictionary<string, int> Fill(PupilQuestionSetDef def, int count)
    {
        var bank = def.Bank;
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            map[bank.AllItems[i].Id] = 3;
        }

        return map;
    }
}
