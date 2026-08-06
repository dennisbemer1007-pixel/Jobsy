using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class RegionHostRulesTests
{
    [Theory]
    [InlineData("https://Westland.Lobsy.nl/path", "westland.lobsy.nl")]
    [InlineData("westland.lobsy.nl:443", "westland.lobsy.nl")]
    [InlineData("  WESTLAND.LOBSY.NL  ", "westland.lobsy.nl")]
    public void NormalizeHostname_StripsSchemePortAndPath(string raw, string expected)
        => Assert.Equal(expected, RegionHostRules.NormalizeHostname(raw));

    [Theory]
    [InlineData("westland.lobsy.nl", true)]
    [InlineData("localhost", true)]
    [InlineData("demo.jobsy.local", true)]
    [InlineData("not a host", false)]
    [InlineData("", false)]
    [InlineData("nodots", false)]
    public void IsValidHostname_ChecksLabels(string host, bool expected)
        => Assert.Equal(expected, RegionHostRules.IsValidHostname(host));
}
