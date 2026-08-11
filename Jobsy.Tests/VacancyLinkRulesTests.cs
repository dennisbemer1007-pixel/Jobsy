using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class VacancyLinkRulesTests
{
    [Theory]
    [InlineData("Kassière", "Leuke bijbaan in Naaldwijk", false)]
    [InlineData("Bekijk https://evil.example", "Tekst", true)]
    [InlineData("Titel", "Solliciteer via www.example.com", true)]
    [InlineData("Titel", "Mail ons of bezoek <a href=\"https://x.nl\">site</a>", true)]
    [InlineData("Werk bij Demo.nl vestiging", "Geen link, alleen naam", true)]
    public void Detects_forbidden_links(string title, string description, bool expected)
    {
        Assert.Equal(expected, VacancyLinkRules.ContainsForbiddenLink(title, description));
    }

    [Fact]
    public void ValidateNoLinks_returns_message()
    {
        var error = VacancyLinkRules.ValidateNoLinks("https://spam.nl", "ok");
        Assert.Equal(VacancyLinkRules.ErrorMessage, error);
    }
}
