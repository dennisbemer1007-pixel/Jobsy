using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class InitiativeCandidateDisplayTests
{
    [Fact]
    public void ShortCode_is_stable_k_prefix()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var code = InitiativeCandidateDisplay.ShortCode(id);
        Assert.Matches(@"^K-\d{4}$", code);
        Assert.Equal(code, InitiativeCandidateDisplay.ShortCode(id));
    }

    [Fact]
    public void EmployerListLabel_uses_short_code_not_guid_fragment()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var label = InitiativeCandidateDisplay.EmployerListLabel(id);
        Assert.StartsWith("Kandidaat K-", label, StringComparison.Ordinal);
        Assert.DoesNotContain("#", label, StringComparison.Ordinal);
        Assert.DoesNotContain("aaaa", label, StringComparison.OrdinalIgnoreCase);
    }
}
