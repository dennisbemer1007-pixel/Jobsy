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

    [Fact]
    public void DisplayFirstName_rejects_email_and_local_part()
    {
        const string email = "dbemer@live.com";
        Assert.Equal(string.Empty, CandidateNameRules.DisplayFirstName(email, null, email));
        Assert.Equal(string.Empty, CandidateNameRules.DisplayFirstName("dbemer", null, email));
        Assert.Equal("Dennis", CandidateNameRules.DisplayFirstName("Dennis", null, email));
    }

    [Fact]
    public void ResolveOnboardingPrefill_splits_display_name_and_skips_email()
    {
        var (first, last) = CandidateNameRules.ResolveOnboardingPrefill(
            "dbemer@live.com",
            null,
            "Ada Lovelace",
            "dbemer@live.com");
        Assert.Equal("Ada", first);
        Assert.Equal("Lovelace", last);

        (first, last) = CandidateNameRules.ResolveOnboardingPrefill(
            "dbemer@live.com",
            "",
            "dbemer@live.com",
            "dbemer@live.com");
        Assert.Equal(string.Empty, first);
        Assert.Equal(string.Empty, last);
    }
}
