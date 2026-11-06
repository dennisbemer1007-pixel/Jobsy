using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

public class PersonalDataMaskerTests
{
    [Theory]
    [InlineData("jan.jansen@x.nl", "j***@x.nl")]
    [InlineData("a@b.nl", "*@b.nl")]
    [InlineData("", "")]
    public void MaskEmail_hides_local_part(string input, string expected)
        => Assert.Equal(expected, PersonalDataMasker.MaskEmail(input));

    [Theory]
    [InlineData("Jan Jansen", "Jan J.")]
    [InlineData("Madonna", "Madonna")]
    [InlineData("Anna Maria de Vries", "Anna V.")]
    public void MaskName_keeps_first_and_last_initial(string input, string expected)
        => Assert.Equal(expected, PersonalDataMasker.MaskName(input));

    [Fact]
    public void MaskPhone_keeps_last_two_digits()
        => Assert.Equal("••• ••• 67", PersonalDataMasker.MaskPhone("+31 6 12345667"));

    [Fact]
    public void MaskIban_keeps_country_and_last_four()
        => Assert.Equal("NL•• •••• •••• 7890", PersonalDataMasker.MaskIban("NL91 ABNA 0417 1643 7890"));

    [Fact]
    public void MaskAddressToCity_prefers_city_argument()
        => Assert.Equal("Utrecht", PersonalDataMasker.MaskAddressToCity("Straat 1", "Utrecht"));

    [Theory]
    [InlineData(17, "<18")]
    [InlineData(22, "18-24")]
    [InlineData(40, "35-44")]
    [InlineData(70, "65+")]
    public void MaskAgeBand_groups(int age, string band)
        => Assert.Equal(band, PersonalDataMasker.MaskAgeBand(age));
}
