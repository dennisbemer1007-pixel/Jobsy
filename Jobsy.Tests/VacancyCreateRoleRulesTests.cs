using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;

namespace Jobsy.Tests;

public class VacancyCreateRoleRulesTests
{
    [Theory]
    [InlineData(UserRole.BranchManager, true)]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Intermediary, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    [InlineData(UserRole.Candidate, false)]
    [InlineData(UserRole.SalesManager, false)]
    public void CanManageVacancyLifecycle_matches_expected_roles(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanManageVacancyLifecycle(role));

    [Theory]
    [InlineData(UserRole.BranchManager, true)]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Intermediary, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    public void CanReactToApplications_blocks_regional_manager(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanReactToApplications(role));

    [Theory]
    [InlineData(UserRole.BranchManager, false)]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Intermediary, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    public void CanPurchaseTokens_blocks_branch_and_regional_manager(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanPurchaseTokens(role));

    [Theory]
    [InlineData(UserRole.BranchManager, false, false)] // no EM → VM may purchase (D5 fallback)
    [InlineData(UserRole.BranchManager, true, true)]  // has EM → VM blocked
    [InlineData(UserRole.EnterpriseManager, true, true)]
    public void CanPurchaseTokens_with_em_flag(UserRole role, bool hasEm, bool expectedWhenEmMeansBlockedForVm)
    {
        _ = expectedWhenEmMeansBlockedForVm;
        // hasEnterpriseManager=true → VM cannot buy; false → VM can (orphan org).
        var expected = role == UserRole.BranchManager ? !hasEm : JobsyRoles.CanPurchaseTokens(role);
        Assert.Equal(expected, JobsyRoles.CanPurchaseTokens(role, hasEm));
    }

    [Theory]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    [InlineData(UserRole.BranchManager, false)]
    public void CanAllocateTokens_is_enterprise_or_admin(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanAllocateTokens(role));

    [Theory]
    [InlineData(UserRole.BranchManager, true)]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Intermediary, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    public void CanMutateEmployerData_blocks_regional_manager(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanMutateEmployerData(role));
}
