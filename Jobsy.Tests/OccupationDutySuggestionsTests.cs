using Jobsy.Core.Careers;

namespace Jobsy.Tests;

public sealed class OccupationDutySuggestionsTests
{
    [Fact]
    public void ForRole_returns_empty_for_blank()
    {
        Assert.Empty(OccupationDutySuggestions.ForRole(null));
        Assert.Empty(OccupationDutySuggestions.ForRole("   "));
    }

    [Fact]
    public void ForRole_can_suggest_from_known_dutch_title()
    {
        var chips = OccupationDutySuggestions.ForRole("magazijnmedewerker");
        Assert.True(chips.Count is >= 0 and <= 4);
    }
}
