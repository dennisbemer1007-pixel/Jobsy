using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class RegistrationPasswordRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short1")]
    [InlineData("allletters")]
    [InlineData("12345678")]
    public void Rejects_weak_passwords(string? password)
        => Assert.Throws<ArgumentException>(() => RegistrationPasswordRules.Validate(password));

    [Fact]
    public void Accepts_letter_and_digit_password()
        => RegistrationPasswordRules.Validate("TestPass1!");
}
