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
}
