using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class CultureStateRequestTests
{
    [Fact]
    public void Lang_query_beats_cookie_beats_nl()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Cookie = "Jobsy.Culture=en";
        Assert.Equal("en", CultureRequest.ResolveLanguage(http));

        http.Request.QueryString = new QueryString("?lang=pl");
        Assert.Equal("pl", CultureRequest.ResolveLanguage(http));

        var empty = new DefaultHttpContext();
        Assert.Equal("nl", CultureRequest.ResolveLanguage(empty));
    }

    [Fact]
    public void InitializeFromRequest_keeps_value_for_InitializeAsync()
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?lang=en");
        var js = new NoJs();
        var culture = new CultureState(js, new ServiceCollection().BuildServiceProvider(), new AnonAuth());
        culture.InitializeFromRequest(http);
        Assert.Equal("en", culture.Language);

        // Second init path must not overwrite.
        culture.InitializeFromRequest(new DefaultHttpContext());
        Assert.Equal("en", culture.Language);
    }

    private sealed class NoJs : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException("js");
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException("js");
    }

    private sealed class AnonAuth : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
    {
        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())));
    }
}
