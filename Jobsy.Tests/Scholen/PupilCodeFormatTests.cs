using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilCodeFormatTests
{
    [Theory]
    [InlineData("k7q-m2p", "K7QM2P")]
    [InlineData(" K7Q M2P ", "K7QM2P")]
    [InlineData("K7QM2P", "K7QM2P")]
    public void Normalize_accepts_case_space_dash(string input, string expected)
        => Assert.Equal(expected, PupilCodeFormat.Normalize(input));

    [Theory]
    [InlineData("K7O-M2P")] // O not in alphabet
    [InlineData("K7Q-M2")]
    [InlineData("")]
    [InlineData("K7Q1M2P")] // 1 not in alphabet
    public void Normalize_rejects_invalid(string input)
        => Assert.Throws<ArgumentException>(() => PupilCodeFormat.Normalize(input));

    [Fact]
    public void Display_formats_with_dash()
        => Assert.Equal("K7Q-M2P", PupilCodeFormat.Display("k7qm2p"));

    [Fact]
    public void Alphabet_excludes_ambiguous_chars()
    {
        Assert.DoesNotContain('I', PupilCodeFormat.Alphabet);
        Assert.DoesNotContain('L', PupilCodeFormat.Alphabet);
        Assert.DoesNotContain('O', PupilCodeFormat.Alphabet);
        Assert.DoesNotContain('0', PupilCodeFormat.Alphabet);
        Assert.DoesNotContain('1', PupilCodeFormat.Alphabet);
        Assert.Equal(31, PupilCodeFormat.Alphabet.Length);
    }
}
