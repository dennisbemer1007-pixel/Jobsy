using Jobsy.Web.Features;

namespace Jobsy.Tests;

public sealed class ReferenceVerificationPublicPeriodDisplayTests
{
    [Fact]
    public void HumanRange_dutch_current_uses_maart_and_nu()
    {
        var text = ReferenceVerificationPublicPeriodDisplay.HumanRange("2021-03", null, isCurrent: true, dutch: true);
        Assert.Equal("maart 2021 tot nu", text);
    }

    [Fact]
    public void HumanRange_english_current()
    {
        var text = ReferenceVerificationPublicPeriodDisplay.HumanRange("2021-03", null, isCurrent: true, dutch: false);
        Assert.Equal("March 2021 to now", text);
    }
}
