using Jobsy.Core.Enums;
using Jobsy.Core.Security;

namespace Jobsy.Tests.TestAccounts;

public class AdminLoginProviderPolicyTestAccountTests
{
    [Fact]
    public void Test_admin_via_google_is_still_refused()
    {
        // Admin Google block (auth 02) is unchanged for test admins.
        Assert.False(AdminLoginProviderPolicy.IsAllowed(
            UserRole.Admin,
            provider: "google",
            entraTenantId: null,
            allowedAdminTenants: ["contoso.onmicrosoft.com"]));
    }
}
