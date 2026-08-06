using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CandidateContactRulesTests
{
    [Fact]
    public void Compose_and_split_names()
    {
        Assert.Equal("Ada Lovelace", CandidateNameRules.ComposeFullName("Ada", "Lovelace"));
        Assert.Equal(("Ada", "Lovelace"), CandidateNameRules.SplitFullName("Ada Lovelace"));
        Assert.Equal("Ada", CandidateNameRules.DisplayFirstName(null, "Ada Lovelace"));
        Assert.Equal("Lovelace", CandidateNameRules.DisplayLastName(null, "Ada Lovelace"));
    }

    [Theory]
    [InlineData("0612345678", true)]
    [InlineData("+31 6 12345678", true)]
    [InlineData("abc", false)]
    [InlineData(null, true)]
    [InlineData("", true)]
    public void Phone_validation(string? phone, bool expected)
        => Assert.Equal(expected, CandidatePhoneRules.IsValid(phone));

    [Fact]
    public void WhatsApp_digits_normalize_nl_mobile()
    {
        Assert.Equal("31612345678", CandidatePhoneRules.ToWhatsAppE164Digits("06 12345678"));
    }
}
