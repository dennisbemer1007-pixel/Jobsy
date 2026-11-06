using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class SchoolYearTests
{
    [Theory]
    [InlineData(2026, 9, 29, 2026)]
    [InlineData(2027, 7, 15, 2026)]
    [InlineData(2027, 7, 31, 2026)]
    [InlineData(2027, 8, 1, 2027)]
    public void Current_uses_july_31_default(int y, int m, int d, int expectedStart)
        => Assert.Equal(expectedStart, SchoolYear.Current(new DateOnly(y, m, d)));

    [Fact]
    public void Label_and_EndsOn()
    {
        Assert.Equal("2026–2027", SchoolYear.Label(2026));
        Assert.Equal(new DateOnly(2027, 7, 31), SchoolYear.EndsOn(2026));
    }

    [Fact]
    public void ValidateCutoff_rejects_invalid_day()
        => Assert.Throws<ArgumentOutOfRangeException>(() => SchoolYear.ValidateCutoff(2, 31));
}
