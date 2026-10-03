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

    private Dictionary<string, int> Fill(int count)
    {
        var bank = _g78.Bank;
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            map[bank.AllItems[i].Id] = 3;
        }

        return map;
    }
}
