using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

public class ConsentVersionTests
{
    [Fact]
    public void IsCurrentConsent_matches_exact_current_version_only()
    {
        Assert.True(PrivacyConstants.IsCurrentConsent(PrivacyConstants.CurrentConsentVersion));
        Assert.False(PrivacyConstants.IsCurrentConsent("2026-07-29"));
        Assert.False(PrivacyConstants.IsCurrentConsent(null));
        Assert.False(PrivacyConstants.IsCurrentConsent(""));
    }

    [Theory]
    [InlineData(UserRole.BranchManager, "2026-07-29", true)]
    [InlineData(UserRole.EnterpriseManager, null, true)]
    [InlineData(UserRole.Admin, "2026-07-29", true)]
    [InlineData(UserRole.SalesManager, "CURRENT", false)]
    [InlineData(UserRole.SalesManager, "2026-08-19", true)]
    [InlineData(UserRole.Candidate, null, false)]
    [InlineData(UserRole.Candidate, "2026-07-29", false)]
    public void RequiresAccountConsentReaccept_by_role_and_version(
        UserRole role,
        string? version,
        bool expected)
    {
        if (version == "CURRENT")
        {
            version = PrivacyConstants.CurrentConsentVersion;
        }

        Assert.Equal(expected, PrivacyConstants.RequiresAccountConsentReaccept(role, version));
    }
}
