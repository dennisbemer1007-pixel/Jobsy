using System.Security.Claims;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public class ApiAuthRedirectGuardTests
{
    [Fact]
    public void Anonymous_html_navigation_must_not_redirect_to_login_on_api_401()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Accept = "text/html,application/xhtml+xml";
        http.Request.Path = "/";
        http.User = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(JobsyApiAuthHandler.ShouldRedirectHtmlNavigationToLogin(http));
    }

    [Fact]
    public void Login_path_must_not_redirect_to_itself_on_api_401()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Accept = "text/html";
        http.Request.Path = "/login";
        http.User = Authed();

        Assert.False(JobsyApiAuthHandler.ShouldRedirectHtmlNavigationToLogin(http));
    }

    [Fact]
    public void Authenticated_html_navigation_may_redirect_to_login_on_api_401()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Accept = "text/html";
        http.Request.Path = "/employer/vacancies";
        http.User = Authed();

        Assert.True(JobsyApiAuthHandler.ShouldRedirectHtmlNavigationToLogin(http));
    }

    [Fact]
    public void Json_api_calls_do_not_trigger_html_login_redirect()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Accept = "application/json";
        http.Request.Path = "/employer/vacancies";
        http.User = Authed();

        Assert.False(JobsyApiAuthHandler.ShouldRedirectHtmlNavigationToLogin(http));
    }

    private static ClaimsPrincipal Authed()
        => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "demo"), new Claim(ClaimTypes.Role, "Candidate")],
            authenticationType: "test"));
}
