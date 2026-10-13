using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Localization;
using Jobsy.Web.Components.Pages.Leraar;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public class SchoolRun7LayoutTests : BunitContext
{
    public SchoolRun7LayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddSingleton<IFeatureFlags>(new SchoolsOn());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new EmptyHandler())
        {
            BaseAddress = new Uri("http://api.test/")
        }));
    }

    [Fact]
    public void Pupil_and_school_shells_link_privacy_and_accessibility()
    {
        var pupil = Render<LeerlingLayout>();
        Assert.Contains("href=\"/privacy\"", pupil.Markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/toegankelijkheid\"", pupil.Markup, StringComparison.Ordinal);
        Assert.Contains("Privacyverklaring", pupil.Markup, StringComparison.Ordinal);
        Assert.Contains("Toegankelijkheid", pupil.Markup, StringComparison.Ordinal);

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo("http://localhost/school");
        var school = Render<SchoolLayout>();
        Assert.Contains("href=\"/privacy\"", school.Markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/toegankelijkheid\"", school.Markup, StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class SchoolsOn : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(false, false, SchoolsEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class EmptyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
    }
}

public class SchoolRun7CodeDetailTests : BunitContext
{
    private static readonly Guid ClassId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CodeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public SchoolRun7CodeDetailTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new SchoolsOn());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new DetailHandler())
        {
            BaseAddress = new Uri("http://api.test/")
        }));
    }

    [Fact]
    public async Task Empty_dislikes_hide_the_heading()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var cut = Render<LeraarCodeDetail>(p => p
            .Add(x => x.ClassId, ClassId)
            .Add(x => x.CodeId, CodeId));
        Assert.Contains("Vindt het leuk", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Vindt het niet leuk", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class SchoolsOn : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(false, false, SchoolsEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class DetailHandler : HttpMessageHandler
    {
        private static readonly string Json = JsonSerializer.Serialize(
            new TeacherCodeDetailDto(
                CodeId,
                ClassId,
                "K7Q-M2P",
                "2B",
                PupilCodeStatus.Completed,
                10,
                10,
                DateTime.UtcNow,
                12,
                new PupilStoryViewDto("Dit ben jij", "Je helpt graag.", [], [], []),
                ["dieren"],
                [],
                null,
                null,
                [],
                null,
                false),
            JobsyApiClient.ApiJson);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Json, Encoding.UTF8, "application/json")
            });
    }
}
