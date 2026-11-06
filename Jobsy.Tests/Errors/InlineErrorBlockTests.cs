using Bunit;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Errors;

/// <summary>errors 04 §04.4: the small "Dit stukje laadt nu niet." card.</summary>
public class InlineErrorBlockTests : BunitContext
{
    private const string Secret = "Secret boom detail from the database connection string.";

    public InlineErrorBlockTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        Services.AddSingleton(sp => new UserFacingError(
            NullLogger<UserFacingError>.Instance,
            sp.GetRequiredService<CultureState>(),
            sp.GetRequiredService<IHttpContextAccessor>()));
    }

    [Fact]
    public void Shows_the_calm_line_and_announces_itself_politely()
    {
        var cut = Render<InlineErrorBlock>();

        var card = cut.Find(".err-inline");
        Assert.Equal("status", card.GetAttribute("role"));
        Assert.Equal(UiStrings.Get("Status.Inline.Title", "nl"), cut.Find(".err-inline__text").TextContent);
        Assert.Equal("true", cut.Find(".err-inline__icon").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll(".err-inline__code"));
        Assert.Empty(cut.FindAll(".err-inline__retry"));
    }

    [Fact]
    public void Shows_the_support_code_the_api_error_carried()
    {
        var cut = Render<InlineErrorBlock>(p => p
            .Add(x => x.Error, new ApiErrorException(ApiErrorException.RateLimited, 429, "LB-7Q3K", 30)));

        var code = cut.Find(".err-inline__code");
        Assert.Equal("LB-7Q3K", code.GetAttribute("data-support-code"));
        Assert.Contains("LB-7Q3K", code.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Mints_a_support_code_for_an_error_without_one()
    {
        var cut = Render<InlineErrorBlock>(p => p
            .Add(x => x.Error, new InvalidOperationException(Secret)));

        var shown = cut.Find(".err-inline__code").GetAttribute("data-support-code");
        Assert.True(Jobsy.Web.Diagnostics.SupportCode.IsValid(shown));
    }

    [Fact]
    public void Never_shows_exception_text()
    {
        var cut = Render<InlineErrorBlock>(p => p
            .Add(x => x.Error, new InvalidOperationException(Secret)));

        Assert.DoesNotContain(Secret, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Retry_button_only_appears_with_a_callback_and_fires_it()
    {
        var clicks = 0;
        var cut = Render<InlineErrorBlock>(p => p
            .Add(x => x.Error, new HttpRequestException(Secret))
            .Add(x => x.OnRetry, EventCallback.Factory.Create(this, () => clicks++)));

        var button = cut.Find(".err-inline__retry");
        Assert.Equal(UiStrings.Get("Status.Inline.Retry", "nl"), button.TextContent);

        button.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Shows_a_validation_message_the_api_wrote_for_users()
    {
        var cut = Render<InlineErrorBlock>(p => p
            .Add(x => x.Error, new ApiErrorException(ApiErrorException.Validation, 400, userMessage: "Vul je postcode in.")));

        Assert.Equal("Vul je postcode in.", cut.Find(".err-inline__detail").TextContent);
    }

    private sealed class AnonymousAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity())));
    }
}
