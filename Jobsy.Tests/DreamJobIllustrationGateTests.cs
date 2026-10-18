using Jobsy.Core.Options;

namespace Jobsy.Tests;

public class DreamJobIllustrationGateTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    public void IsEnabled_parses_config(string? raw, bool expected)
        => Assert.Equal(expected, DreamJobIllustrationGate.IsEnabled(raw));
}
