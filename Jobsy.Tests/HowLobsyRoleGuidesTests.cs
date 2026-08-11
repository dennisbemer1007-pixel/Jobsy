using Jobsy.Core.Authorization;
using Jobsy.Web.Help;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class HowLobsyRoleGuidesTests
{
    [Theory]
    [InlineData(JobsyRoles.Candidate)]
    [InlineData(JobsyRoles.BranchManager)]
    [InlineData(JobsyRoles.RegionalManager)]
    [InlineData(JobsyRoles.EnterpriseManager)]
    [InlineData(JobsyRoles.Intermediary)]
    [InlineData(JobsyRoles.SalesManager)]
    [InlineData(JobsyRoles.Ambassadeur)]
    public void ForRole_returns_guide_with_localized_keys(string role)
    {
        var guide = HowLobsyRoleGuides.ForRole(role);
        Assert.NotNull(guide);
        Assert.NotEmpty(guide!.Steps);
        Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get(guide.TitleKey, "nl")));
        Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get(guide.LeadKey, "nl")));
        Assert.NotEqual(guide.TitleKey, UiStrings.Get(guide.TitleKey, "nl"));
        foreach (var step in guide.Steps)
        {
            Assert.NotEqual(step.TitleKey, UiStrings.Get(step.TitleKey, "nl"));
            Assert.NotEqual(step.BodyKey, UiStrings.Get(step.BodyKey, "nl"));
        }
    }

    [Fact]
    public void ForRole_admin_has_no_guide()
        => Assert.Null(HowLobsyRoleGuides.ForRole(JobsyRoles.Admin));

    [Fact]
    public void BuildSalesGuide_uses_personal_partner_path_when_code_present()
    {
        var withCode = HowLobsyRoleGuides.BuildSalesGuide("SM-ABC");
        var partnerStep = withCode.Steps[2];
        Assert.Equal("/partner/SM-ABC", partnerStep.Links[0].Href);

        var withoutCode = HowLobsyRoleGuides.BuildSalesGuide(null);
        Assert.Equal("/salesmanager/toolkit", withoutCode.Steps[2].Links[0].Href);
    }

    [Fact]
    public void BuildAmbassadeurGuide_uses_werven_path_when_code_present()
    {
        var withCode = HowLobsyRoleGuides.BuildAmbassadeurGuide("AM-DEMO01");
        Assert.Equal("/werven/AM-DEMO01", withCode.Steps[2].Links[0].Href);
        Assert.Equal("/ambassadeur/toolkit", withCode.Primary.Href);

        var withoutCode = HowLobsyRoleGuides.BuildAmbassadeurGuide(null);
        Assert.Equal("/ambassadeur/toolkit", withoutCode.Steps[2].Links[0].Href);
    }
}
