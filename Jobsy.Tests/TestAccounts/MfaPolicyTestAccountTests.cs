using Jobsy.Core.Enums;
using Jobsy.Core.Security;

namespace Jobsy.Tests.TestAccounts;

public class MfaPolicyTestAccountTests
{
    [Fact]
    public void Test_admin_with_runtime_active_skips_mfa()
    {
        Assert.False(MfaPolicy.IsRequiredFor(UserRole.Admin, isTestAccount: true, testAccountsActive: true));
    }

    [Fact]
    public void Test_admin_with_runtime_inactive_requires_mfa()
    {
        Assert.True(MfaPolicy.IsRequiredFor(UserRole.Admin, isTestAccount: true, testAccountsActive: false));
    }

    [Fact]
    public void Real_admin_always_requires_mfa()
    {
        Assert.True(MfaPolicy.IsRequiredFor(UserRole.Admin, isTestAccount: false, testAccountsActive: true));
        Assert.True(MfaPolicy.IsRequiredFor(UserRole.Admin, isTestAccount: false, testAccountsActive: false));
    }

    [Fact]
    public void Production_row_flagged_test_still_requires_mfa_when_runtime_inactive()
    {
        Assert.True(MfaPolicy.IsRequiredFor(UserRole.BranchManager, isTestAccount: true, testAccountsActive: false));
    }

    [Fact]
    public void IsRequired_unchanged_for_auth_stack_roles()
    {
        Assert.True(MfaPolicy.IsRequired(UserRole.SalesManager));
        Assert.True(MfaPolicy.IsRequired(UserRole.Teacher));
        Assert.True(MfaPolicy.IsRequired(UserRole.SchoolAdmin));
        Assert.False(MfaPolicy.IsRequired(UserRole.Ambassadeur));
        Assert.False(MfaPolicy.IsRequired(UserRole.Candidate));
    }
}
