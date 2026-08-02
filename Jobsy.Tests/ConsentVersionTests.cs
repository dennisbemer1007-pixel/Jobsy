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
    [InlineData(UserRole.SalesManager, "2026-08-02", false)]
    [InlineData(UserRole.Candidate, null, false)]
    [InlineData(UserRole.Candidate, "2026-07-29", false)]
    public void RequiresAccountConsentReaccept_by_role_and_version(
        UserRole role,
        string? version,
        bool expected)
    {
        // Keep theory in sync with the live constant for the "current" case.
        if (version == "2026-08-02")
        {
            version = PrivacyConstants.CurrentConsentVersion;
            expected = false;
        }

        Assert.Equal(expected, PrivacyConstants.RequiresAccountConsentReaccept(role, version));
    }
}
