using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public class CandidateGuestRedirectMiddlewareTests
{
    [Theory]
    [InlineData("/candidate/start")]
    [InlineData("/candidate/paspoort")]
    public async Task Guest_get_is_redirected_to_login(string path)
    {
        var middleware = new Jobsy.Web.Auth.CandidateGuestRedirectMiddleware(_ =>
        {
            Assert.Fail("Next should not run for guest redirect.");
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        var location = context.Response.Headers.Location.ToString();
        Assert.Contains("/login?returnUrl=", location, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString(path), location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authenticated_request_passes_through()
    {
        var seen = false;
        var middleware = new Jobsy.Web.Auth.CandidateGuestRedirectMiddleware(_ =>
        {
            seen = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/candidate/start";
        context.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("sub", "guest-test")],
                "test"));

        await middleware.InvokeAsync(context);

        Assert.True(seen);
    }
}
