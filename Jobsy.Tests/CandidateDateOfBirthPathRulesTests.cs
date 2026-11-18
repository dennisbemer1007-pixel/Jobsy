using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

public sealed class CandidateDateOfBirthPathRulesTests
{
    [Theory]
    [InlineData("/candidate/kompas", null, true)]
    [InlineData("/candidate/paspoort", "proof", true)]
    [InlineData("/candidate/paspoort", "data", false)]
    [InlineData("/candidate/start", null, false)]
    [InlineData("/candidate/settings", null, false)]
    [InlineData("/candidate/geboortedatum", null, false)]
    [InlineData("/candidate/privacy", null, false)]
    public void Redirect_rules(string path, string? tab, bool expectRedirect)
    {
        Assert.Equal(expectRedirect, CandidateDateOfBirthPathRules.ShouldRedirect(path, tab, hasDateOfBirth: false));
    }
}
