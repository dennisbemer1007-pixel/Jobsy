using Jobsy.Web.Auth;

namespace Jobsy.Tests;

public class EntraAuthFailureFormatterTests
{
    [Fact]
    public void TryGetAadstsCode_finds_code_in_message_and_inner()
    {
        var inner = new InvalidOperationException("AADSTS50011: redirect_uri mismatch");
        var outer = new InvalidOperationException("OAuth token exchange failed", inner);

        Assert.Equal("AADSTS50011", EntraAuthFailureFormatter.TryGetAadstsCode(outer));
        Assert.Contains("AADSTS50011", EntraAuthFailureFormatter.Describe(outer), StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_truncates_long_messages()
    {
        var ex = new InvalidOperationException(new string('x', 600));
        var text = EntraAuthFailureFormatter.Describe(ex);
        Assert.True(text.Length <= 505);
        Assert.EndsWith("…", text, StringComparison.Ordinal);
    }
}
