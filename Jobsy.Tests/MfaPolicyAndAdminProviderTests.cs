using Jobsy.Core.Enums;
using Jobsy.Core.Security;

namespace Jobsy.Tests;

public class MfaPolicyTests
{
    public static IEnumerable<object[]> AllRoles()
        => Enum.GetValues<UserRole>().Select(r => new object[] { r });

    [Theory]
    [MemberData(nameof(AllRoles))]
    public void IsRequired_matches_policy(UserRole role)
    {
        var expected = role is UserRole.Admin
            or UserRole.BranchManager
            or UserRole.RegionalManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary
            or UserRole.SchoolAdmin
            or UserRole.Teacher
            or UserRole.SalesManager;
        Assert.Equal(expected, MfaPolicy.IsRequired(role));
    }
}

public class AdminLoginProviderPolicyTests
{
    [Fact]
    public void Non_admin_always_allowed()
    {
        Assert.True(AdminLoginProviderPolicy.IsAllowed(UserRole.Candidate, "google", null, []));
        Assert.True(AdminLoginProviderPolicy.IsAllowed(UserRole.SalesManager, "google", null, []));
    }

    [Fact]
    public void Admin_google_blocked()
        => Assert.False(AdminLoginProviderPolicy.IsAllowed(UserRole.Admin, "google", null, []));

    [Fact]
    public void Admin_personal_microsoft_blocked()
        => Assert.False(AdminLoginProviderPolicy.IsAllowed(
            UserRole.Admin,
            "entra",
            AdminLoginProviderPolicy.PersonalMicrosoftTenantId,
            []));

    [Fact]
    public void Admin_work_tenant_allowed_when_list_empty()
        => Assert.True(AdminLoginProviderPolicy.IsAllowed(
            UserRole.Admin, "entra", "11111111-1111-1111-1111-111111111111", []));

    [Fact]
    public void Admin_allowed_tenants_respected()
    {
        var allowed = new[] { "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" };
        Assert.True(AdminLoginProviderPolicy.IsAllowed(
            UserRole.Admin, "entra", allowed[0], allowed));
        Assert.False(AdminLoginProviderPolicy.IsAllowed(
            UserRole.Admin, "entra", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", allowed));
    }

    [Fact]
    public void Admin_local_password_allowed()
        => Assert.True(AdminLoginProviderPolicy.IsAllowed(UserRole.Admin, null, null, []));
}
