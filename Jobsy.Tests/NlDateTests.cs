using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class NlDateTests
{
    [Fact]
    public void Day_formats_in_amsterdam_without_throwing()
    {
        var utc = new DateTime(2026, 10, 12, 22, 30, 0, DateTimeKind.Utc);
        var formatted = NlDate.Day(utc);
        Assert.Matches(@"^\d{2}-\d{2}-\d{4}$", formatted);
        Assert.Equal("13-10-2026", formatted);
    }
}
