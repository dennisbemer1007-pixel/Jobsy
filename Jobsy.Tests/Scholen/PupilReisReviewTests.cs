using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilReisReviewTests
{
    [Fact]
    public void WithPreviousQuestion_shows_prior_item_without_mutating_answers()
    {
        var def = new PupilQuestionSetRegistry().Get(PupilQuestionSet.Groep78);
        var first = def.Bank.AllItems[0];
        var second = def.Bank.AllItems[1];
        var progress = new PupilProgressStateDto(
            Guid.NewGuid(),
            "8B",
            "ABC-DEF",
            PupilCodeStatus.InProgress,
            second.GlobalIndex,
            def.Bank.AllItems.Count,
            2,
            0,
            false,
            second.Id,
            second.WorldKey,
            new Dictionary<string, int> { [first.Id] = 3, [second.Id] = 4 },
            true,
            false,
            false,
            false,
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null,
            PupilQuestionSet.Groep78);

        var review = PupilReisReview.WithPreviousQuestion(progress, def);
        Assert.NotNull(review);
        Assert.Equal(first.Id, review!.CurrentItemId);
        Assert.Equal(first.GlobalIndex, review.CurrentIndex);
        Assert.Equal(second.Id, progress.CurrentItemId);
    }
}
