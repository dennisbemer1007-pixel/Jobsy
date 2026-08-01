using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class HighlightShuffleRulesTests
{
    [Fact]
    public void Rank_is_stable_for_same_seed_and_id()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.Equal(HighlightShuffleRules.Rank(42, id), HighlightShuffleRules.Rank(42, id));
    }

    [Fact]
    public void Rank_changes_with_seed_or_id()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var b = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.NotEqual(HighlightShuffleRules.Rank(1, a), HighlightShuffleRules.Rank(2, a));
        Assert.NotEqual(HighlightShuffleRules.Rank(1, a), HighlightShuffleRules.Rank(1, b));
    }
}
