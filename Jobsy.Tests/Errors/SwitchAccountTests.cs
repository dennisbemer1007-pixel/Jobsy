using System.Net;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Errors 02.5 (Dependency E absent on acceptatie): "Inloggen met een ander account" signs out
/// and lands on /login with the kept return URL. Never an open redirect.
/// </summary>
public class SwitchAccountTests
{
    [Fact]
    public async Task Post_logout_with_reason_switch_signs_out_and_redirects_to_login_with_return_url()
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.PostAsync(
            "/account/logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["reason"] = "switch",
                ["returnUrl"] = "/admin/users"
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Fadmin%2Fusers", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Get_logout_with_reason_switch_does_the_same_for_bookmarks()
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/account/logout?reason=switch&returnUrl=%2Fadmin%2Fusers");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Fadmin%2Fusers", response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example")]
    public async Task Absolute_or_schemeless_double_slash_return_url_never_leaves_the_site(string evil)
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.PostAsync(
            "/account/logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["reason"] = "switch",
                ["returnUrl"] = evil
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Fhome", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Plain_logout_without_a_reason_still_goes_to_the_homepage()
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/account/logout");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }
}
