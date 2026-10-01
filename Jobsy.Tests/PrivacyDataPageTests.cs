using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
using Jobsy.Web.Components.Pages.Legal;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// <c>/privacy/data</c> (public-pages 07): the page is prerendered content (not "Laden…"), the
/// support-access card shows Europe/Amsterdam date/time, the empty state has no raw audit copy,
/// and the markup carries no inline <c>style=</c> (design-system rule).
/// </summary>
public class PrivacyDataPageTests : TestContext
{
    public PrivacyDataPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AuthenticatedAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    private void RegisterApi(IReadOnlyList<DateTime> notes)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(notes);
        var http = new HttpClient(new JsonHandler(json)) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new JobsyApiClient(http));
    }

    private void RegisterFailingApi()
    {
        var http = new HttpClient(new FailingHandler()) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new JobsyApiClient(http));
    }

    [Fact]
    public void Page_renders_hero_and_cards_without_a_loading_placeholder()
    {
        RegisterApi([]);
        var cut = RenderComponent<PrivacyData>();

        Assert.Single(cut.FindAll("h1"));
        Assert.Contains("Mijn gegevens", cut.Find("h1").TextContent);
        Assert.DoesNotContain("Laden", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            cut.FindAll("a"),
            a => a.GetAttribute("href") == "/privacy/data/export" && a.HasAttribute("download"));
        Assert.DoesNotContain(cut.Markup, "<pre");
    }

    [Fact]
    public void Support_note_times_use_Europe_Amsterdam_offsets()
    {
        RegisterApi([
            new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc)
        ]);

        var cut = RenderComponent<PrivacyData>();
        var markup = cut.Markup;

        Assert.Contains("14:00", markup, StringComparison.Ordinal);
        Assert.Contains("13:00", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_state_shows_the_translated_sentence_not_the_admin_audit_string()
    {
        RegisterApi([]);
        var cut = RenderComponent<PrivacyData>();

        Assert.Contains("Niemand van Lobsy heeft je gegevens bekeken.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Support_card_is_hidden_when_the_api_call_fails_instead_of_a_raw_error()
    {
        RegisterFailingApi();
        var cut = RenderComponent<PrivacyData>();

        Assert.DoesNotContain("Wie heeft je gegevens bekeken?", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Markup_has_no_inline_styles()
    {
        RegisterApi([]);
        var cut = RenderComponent<PrivacyData>();

        Assert.DoesNotContain("style=", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_failed_query_shows_the_translated_message()
    {
        RegisterApi([]);
        Services.AddSingleton<NavigationManager>(new StaticNavigation("/privacy/data?export=failed"));
        var cut = RenderComponent<PrivacyData>();

        Assert.Contains("Downloaden lukte niet", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class AuthenticatedAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "Test"))));
    }

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(System.Text.Json.JsonSerializer.Deserialize<List<DateTime>>(json))
            });
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }

    private sealed class StaticNavigation : NavigationManager
    {
        public StaticNavigation(string uri) => Initialize("http://localhost/", "http://localhost" + uri);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
