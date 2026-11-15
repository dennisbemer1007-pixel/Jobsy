using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkFeatureIndependenceTests
{
    [Fact]
    public void Employer_loopbaan_apk_does_not_require_employers_feature()
    {
        var snap = new FeatureFlagSnapshot(
            EmployersEnabled: false,
            CandidatePassportEnabled: true,
            EmployerLoopbaanApkEnabled: true);

        Assert.True(snap.IsEnabled(PlatformFeature.EmployerLoopbaanApk));
    }

    [Fact]
    public void Enterprise_manager_sees_only_loopbaan_apk_nav_when_employers_off()
    {
        var snap = new FeatureFlagSnapshot(
            EmployersEnabled: false,
            CandidatePassportEnabled: true,
            EmployerLoopbaanApkEnabled: true);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, JobsyRoles.EnterpriseManager)],
            "Test"));

        var items = RoleNavCatalog.ForUser(user, snap);

        Assert.Single(items);
        Assert.Equal(RoleNavCatalog.LoopbaanApk.Href, items[0].Href);
    }
}
