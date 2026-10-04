using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class AssistantReplyGuardTests
{
    [Fact]
    public void Rejects_internal_test_names_and_a_denial_when_tests_are_done()
    {
        const string facts = "completedTests=Beroepentest; deepTests=none (no extra long test; this does not mean completedTests is empty); ";
        Assert.False(AssistantReplyGuard.Accepts("Your top Riasec is hands-on.", facts));
        Assert.False(AssistantReplyGuard.Accepts("Je hebt de Career-test gedaan.", facts));
        Assert.False(AssistantReplyGuard.Accepts("Je hebt nog geen tests gedaan.", facts));
        Assert.False(AssistantReplyGuard.Accepts("60% on werksterkte.", facts));
        Assert.True(AssistantReplyGuard.Accepts("Je hebt de beroepentest gedaan. Aanpakken met je handen is 66%.", facts));
    }

    [Theory]
    [InlineData("en", "Hands-on work")]
    [InlineData("pl", "Praca rękami")]
    [InlineData("ro", "Lucrul cu mâinile")]
    [InlineData("ar", "العمل باليدين")]
    public void Score_labels_follow_the_reply_language(string lang, string expected)
    {
        Assert.Equal(expected, DimensionLabels.For(CareerTestCatalog.Realistic, lang));
        Assert.DoesNotContain("werksterkte", DimensionLabels.For(CompetencyTestCatalog.Samenwerken, lang), StringComparison.OrdinalIgnoreCase);
    }
}
