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
        Assert.False(CultureRequest.HasExplicitChoice(empty));
        Assert.False(CultureRequest.HasExplicitChoice(null));
        Assert.True(CultureRequest.HasExplicitChoice(http));
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

    [Fact]
    public async Task Js_cookie_wins_and_a_failed_script_does_not_lock_the_language()
    {
        var js = new ScriptedJs { FailNext = true };
        var culture = new CultureState(js, new ServiceCollection().BuildServiceProvider(), new AnonAuth());
        await culture.InitializeAsync();
        Assert.Equal("nl", culture.Language);

        js.Value = "en";
        await culture.InitializeAsync();
        Assert.Equal("en", culture.Language);
    }

    [Fact]
    public async Task Request_cookie_is_used_when_js_is_not_ready()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Cookie = "Jobsy.Culture=ro";
        var accessor = new HttpContextAccessor { HttpContext = http };
        var culture = new CultureState(
            new NoJs(),
            new ServiceCollection().BuildServiceProvider(),
            new AnonAuth(),
            accessor);
        await culture.InitializeAsync();
        Assert.Equal("ro", culture.Language);
    }

    private sealed class ScriptedJs : Microsoft.JSInterop.IJSRuntime
    {
        public bool FailNext { get; set; }
        public string? Value { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("js");
            }

            if (typeof(TValue) == typeof(string))
            {
                return ValueTask.FromResult((TValue)(object)(Value ?? "")!);
            }

            return ValueTask.FromResult(default(TValue)!);
        }
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
