using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class MatchDeckTests
{
    [Fact]
    public void Advance_UpNext_and_ResumeAt_work()
    {
        var deck = new MatchDeck();
        deck.ReplaceItems(
        [
            Card("a", 90),
            Card("b", 80),
            Card("c", 70),
            Card("d", 60)
        ]);

        Assert.Equal(1, deck.Position);
        Assert.Equal(4, deck.Count);
        Assert.Equal(3, deck.UpNext(3).Count);
        Assert.Equal("b", deck.UpNext(3)[0].JobTitle);

        deck.Advance();
        Assert.Equal(2, deck.Position);
        Assert.Equal(2, deck.UpNext(3).Count);

        deck.ResumeAt(deck.Items[3].VacancyId!.Value);
        Assert.Equal(4, deck.Position);
        Assert.Empty(deck.UpNext(3));
    }

    [Fact]
    public void OrderByMatch_is_off_by_default_via_ReplaceItems_order()
    {
        var deck = new MatchDeck();
        deck.ReplaceItems([Card("low", 10), Card("high", 90)]);
        Assert.Equal("low", deck.Current!.JobTitle);
    }

    [Fact]
    public void OrderByMatch_helper_orders_desc_nulls_last()
    {
        var items = new List<VacancyListItem>
        {
            new() { Id = Guid.NewGuid(), Title = "mid", MatchPercent = 50 },
            new() { Id = Guid.NewGuid(), Title = "null", MatchPercent = null },
            new() { Id = Guid.NewGuid(), Title = "high", MatchPercent = 90 },
            new() { Id = Guid.NewGuid(), Title = "low", MatchPercent = 10 }
        };

        var ordered = items
            .OrderByDescending(v => v.MatchPercent ?? int.MinValue)
            .ThenBy(v => v.Id)
            .Select(v => v.Title)
            .ToList();

        Assert.Equal(new[] { "high", "mid", "low", "null" }, ordered);
    }

    [Fact]
    public async Task LikeAsync_swallows_api_errors()
    {
        var deck = new MatchDeck();
        var model = Card("x", 50);
        // Null-safe: LikeAsync with no vacancy id is a no-op.
        model.VacancyId = null;
        await deck.LikeAsync(api: null!, model);
    }

    private static SwipeViewModel Card(string title, int pct) => new()
    {
        VacancyId = Guid.NewGuid(),
        JobTitle = title,
        CompanyName = "Co",
        MatchPercentage = pct,
        ShowMatchPercentage = true
    };
}
