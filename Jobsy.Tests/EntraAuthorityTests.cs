using Jobsy.Web.Auth;

namespace Jobsy.Tests;

public class EntraAuthorityTests
{
    [Theory]
    [InlineData("https://login.microsoftonline.com/common/v2.0", "common")]
    [InlineData("https://login.microsoftonline.com/organizations/v2.0", "organizations")]
    [InlineData("https://login.microsoftonline.com/f3a07784-7295-42f7-ac9f-c417628ec3f5/v2.0", "f3a07784-7295-42f7-ac9f-c417628ec3f5")]
    [InlineData("https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0", "9188040d-6c67-4c5b-b112-36a304b66dad")]
    public void TryGetTenantSegment_parses_authority_path(string authority, string expected)
    {
        Assert.Equal(expected, EntraAuthority.TryGetTenantSegment(authority));
    }

    [Fact]
    public void IsMultitenantAuthoritySegment_recognizes_common_and_organizations()
    {
        Assert.True(EntraAuthority.IsMultitenantAuthoritySegment("common"));
        Assert.True(EntraAuthority.IsMultitenantAuthoritySegment("organizations"));
        Assert.False(EntraAuthority.IsMultitenantAuthoritySegment("f3a07784-7295-42f7-ac9f-c417628ec3f5"));
    }
}
