using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class KvkSbiClassificationTests
{
    [Theory]
    [InlineData("78", true)]
    [InlineData("7810", true)]
    [InlineData("7820", true)]
    [InlineData("7830", true)]
    [InlineData("78.20", true)]
    [InlineData("4711", false)]
    [InlineData("5229", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsIntermediarySbi_detects_prefix_78(string? code, bool expected)
        => Assert.Equal(expected, KvkSbiClassification.IsIntermediarySbi(code));

    [Fact]
    public void IsIntermediary_true_when_any_code_matches()
    {
        Assert.True(KvkSbiClassification.IsIntermediary(["4711", "7820"]));
        Assert.False(KvkSbiClassification.IsIntermediary(["4711", "5610"]));
        Assert.False(KvkSbiClassification.IsIntermediary(null));
    }
}
