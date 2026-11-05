using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilProgressStatePatchTests
{
    [Fact]
    public void After_answer_updates_counts_and_next_step_without_full_get()
    {
        var state = new PupilProgressStateDto(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            "2B",
            "ABCD12",
            PupilCodeStatus.InProgress,
            CurrentIndex: 0,
            TotalItems: 100,
            AnsweredCount: 0,
            PlatesShed: 0,
            NewShell: false,
            CurrentItemId: "9001",
            CurrentWorldKey: "w1",
            Answers: new Dictionary<string, int>(),
            WindowOpen: true,
            Completed: false,
            NeedsIsland: false,
            IslandDone: false,
            VakkenDone: false,
            Likes: [],
            Dislikes: [],
            LikeOtherWord: null,
            DislikeOtherWord: null,
            QuestionSet: PupilQuestionSet.Vo,
            NextStep: "question",
            NextPuzzleKey: null,
            JourneyThemeKey: null,
            JourneyThemeChosenAtUtc: null);

        var answer = new PupilAnswerResponse(
            CurrentIndex: 1,
            AnsweredCount: 1,
            PlatesShed: 0,
            Completed: false,
            NextItemId: "9002",
            NextWorldKey: "w1",
            NeedsIsland: false,
            ResultPending: false,
            NextStep: "question",
            NextPuzzleKey: null);

        var patched = PupilProgressStatePatch.AfterAnswer(state, "9001", 4, answer);

        Assert.Equal(1, patched.AnsweredCount);
        Assert.Equal("9002", patched.CurrentItemId);
        Assert.Equal(4, patched.Answers["9001"]);
    }
}
