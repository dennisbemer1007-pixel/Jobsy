using Jobsy.Api.Security;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class LoginProtectionMiddlewareTests
{
    [Fact]
    public async Task Candidate_apply_post_skips_verification_code_login_protection()
    {
        var limiter = new LoginProtectionRateLimiter();
        const string email = "kandidaat@jobsy.local";
        for (var i = 0; i < 8; i++)
        {
            Assert.True(limiter.TryAcquire("verification-code", "127.0.0.1", email));
        }

        Assert.False(limiter.TryAcquire("verification-code", "127.0.0.1", email));

        var reachedNext = false;
        var middleware = new LoginProtectionMiddleware(_ =>
        {
            reachedNext = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/applications";
        context.Request.ContentType = "application/json";
        var body = $$"""{"vacancyId":"{{Guid.Empty}}","email":"{{email}}"}""";
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
        context.Request.Body.Position = 0;

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.RequestServices = services.BuildServiceProvider();

        await middleware.InvokeAsync(context, limiter);

        Assert.True(reachedNext);
        Assert.NotEqual(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }
}
