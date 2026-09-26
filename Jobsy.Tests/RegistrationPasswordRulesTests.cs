using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class RegistrationPasswordRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("kort")]
    [InlineData("password123!")]
    [InlineData("123456789012")]
    public void Rejects_weak_passwords(string? password)
        => Assert.Throws<ArgumentException>(() => RegistrationPasswordRules.Validate(password));

    [Fact]
    public void Accepts_long_passphrase_without_forced_complexity()
        => RegistrationPasswordRules.Validate("zeeuws landschap");
}
