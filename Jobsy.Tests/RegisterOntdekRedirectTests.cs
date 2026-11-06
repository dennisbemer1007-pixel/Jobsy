using Jobsy.Web.Auth;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public class RegisterOntdekRedirectTests
{
    [Fact]
    public async Task Register_van_ontdek_redirects_to_account_maken()
    {
        var redirected = false;
        var next = new RequestDelegate(_ =>
        {
            redirected = false;
            return Task.CompletedTask;
        });
        var mw = new RegisterOntdekRedirectMiddleware(next);
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = HttpMethods.Get;
        ctx.Request.Path = "/register";
        ctx.Request.QueryString = new QueryString("?van=ontdek");

        await mw.InvokeAsync(ctx);

        Assert.Equal(StatusCodes.Status302Found, ctx.Response.StatusCode);
        Assert.Equal(PublicRoutes.CreateAccountFromTest, ctx.Response.Headers.Location.ToString());
        Assert.False(redirected);
    }

    [Theory]
    [InlineData("/register")]
    [InlineData("/register?ref=SM-ABC")]
    [InlineData("/register?van=other")]
    public async Task Other_register_requests_pass_through(string pathAndQuery)
    {
        var called = false;
        var mw = new RegisterOntdekRedirectMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = HttpMethods.Get;
        var q = pathAndQuery.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            ctx.Request.Path = pathAndQuery[..q];
            ctx.Request.QueryString = new QueryString(pathAndQuery[q..]);
        }
        else
        {
            ctx.Request.Path = pathAndQuery;
        }

        await mw.InvokeAsync(ctx);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
    }

    [Fact]
    public void AuthRedirects_safe_local_rejects_open_redirects()
    {
        Assert.Equal("/home", AuthRedirects.SafeLocalUrl("//evil"));
        Assert.Equal("/home", AuthRedirects.SafeLocalUrl("https://evil.example"));
        Assert.Equal("/home", AuthRedirects.SafeLocalUrl("/home"));
    }

    [Fact]
    public void EmailCodeCookie_masks_email()
    {
        Assert.Equal("d•••@gmail.com", EmailCodeCookie.MaskEmail("demo@gmail.com"));
        Assert.Equal("a•••@x.nl", EmailCodeCookie.MaskEmail("a@x.nl"));
    }
}
