using Jobsy.Core.Enums;
using Jobsy.Core.Ops;

namespace Jobsy.Tests.TestAccounts;

public class TestAccountCatalogTests
{
    [Fact]
    public void Builds_emails_from_fixed_slugs()
    {
        Assert.Equal("test-kandidaat@lobsy.nl", TestAccountCatalog.BuildEmail("kandidaat", "lobsy.nl"));
        Assert.Equal("test-werkgever@example.test", TestAccountCatalog.BuildEmail("werkgever", "example.test"));
    }

    [Fact]
    public void Contains_expected_keys_including_schools()
    {
        var keys = TestAccountCatalog.All.Select(e => e.AccountKey).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Candidate", keys);
        Assert.Contains("CandidateNew", keys);
        Assert.Contains("BranchManager", keys);
        Assert.Contains("Admin", keys);
        Assert.Contains("Ambassadeur", keys);
        Assert.Contains("Teacher", keys);
        Assert.Contains("SchoolAdmin", keys);
        Assert.True(TestAccountCatalog.RoleExistsInBuild(UserRole.Teacher));
    }
}
