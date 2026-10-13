using Jobsy.Core.Security;

namespace Jobsy.Tests;

public class EmailMaskTests
{
    [Theory]
    [InlineData("d.devries@bakkerijzon.nl", "d••••@bakkerijzon.nl")]
    [InlineData("a@x.nl", "a••••@x.nl")]
    [InlineData("", "")]
    public void Mask_keeps_first_letter_and_domain(string input, string expected)
        => Assert.Equal(expected, EmailMask.Mask(input));
}

public class RecoveryCodeFormatTests
{
    [Fact]
    public void Generate_uses_alphabet_length_and_count()
    {
        var codes = MfaRecoveryCodes.Generate();
        Assert.Equal(10, codes.Length);
        foreach (var code in codes)
        {
            Assert.Equal(8, code.Length);
            Assert.All(code, c => Assert.Contains(c, MfaRecoveryCodes.Alphabet));
            Assert.Contains('-', MfaRecoveryCodes.FormatGrouped(code));
            Assert.Equal(9, MfaRecoveryCodes.FormatGrouped(code).Length);
        }
    }

    [Theory]
    [InlineData("7kq2-m9xa", "7KQ2M9XA")]
    [InlineData(" 7KQ2M9XA ", "7KQ2M9XA")]
    [InlineData("7KQ2 M9XA", "7KQ2M9XA")]
    public void Normalize_strips_and_uppercases(string input, string expected)
        => Assert.Equal(expected, MfaRecoveryCodes.Normalize(input));

    [Fact]
    public void Hash_matches_across_grouping_variants()
    {
        var a = MfaRecoveryCodes.Hash("7kq2-m9xa");
        var b = MfaRecoveryCodes.Hash(" 7KQ2M9XA ");
        var c = MfaRecoveryCodes.Hash("7KQ2 M9XA");
        Assert.Equal(a, b);
        Assert.Equal(a, c);
    }

    [Fact]
    public void Old_16_hex_code_still_hashes()
    {
        var hex = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8));
        Assert.Equal(16, hex.Length);
        var hash = MfaRecoveryCodes.Hash(hex);
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Equal(hash, MfaRecoveryCodes.Hash(hex.ToLowerInvariant()));
    }
}
