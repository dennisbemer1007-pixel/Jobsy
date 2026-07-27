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
    [InlineData(UserRole.BranchManager, true)]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Intermediary, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    public void CanPurchaseTokens_blocks_regional_manager(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanPurchaseTokens(role));

    [Theory]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    [InlineData(UserRole.BranchManager, false)]
    public void CanAllocateTokens_is_enterprise_or_admin(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanAllocateTokens(role));
}
