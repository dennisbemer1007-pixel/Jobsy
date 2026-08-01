using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class VacancyHighlightRulesTests
{
    [Fact]
    public void Inactive_when_not_flagged()
    {
        var now = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(VacancyHighlightRules.IsActive(false, now.AddDays(7), now));
    }

    [Fact]
    public void Active_for_legacy_rows_without_until()
    {
        var now = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(VacancyHighlightRules.IsActive(true, null, now));
    }

    [Fact]
    public void Active_before_expiry()
    {
        var now = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(VacancyHighlightRules.IsActive(true, now.AddDays(1), now));
    }

    [Fact]
    public void Inactive_after_expiry()
    {
        var now = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(VacancyHighlightRules.IsActive(true, now.AddMinutes(-1), now));
    }

    [Fact]
    public void ComputeUntil_adds_highlight_window()
    {
        var now = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var until = VacancyHighlightRules.ComputeUntil(now);
        Assert.Equal(now.AddDays(VacancyProductRules.HighlightDays), until);
    }
}
