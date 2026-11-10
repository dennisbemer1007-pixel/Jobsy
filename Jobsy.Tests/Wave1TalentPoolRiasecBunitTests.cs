using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Werkgever.Sections;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class Wave1TalentPoolRiasecHiddenBunitTests : BunitContext
{
    public Wave1TalentPoolRiasecHiddenBunitTests()
    {
        Wave1TalentPoolRiasecBunitSupport.Register(this,
            services => services.AddSingleton(Wave1TalentPoolRiasecBunitSupport.BuildConfiguration(showRiasec: false)));
    }

    [Fact]
    public void Talent_pool_tag_input_hides_riasec_when_config_off()
    {
        var cut = Render<TalentPoolSection>();
        cut.WaitForAssertion(() => Assert.Contains("Zoeken", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("RIASEC", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Samenwerken, Social", cut.Markup, StringComparison.Ordinal);
    }
}

public sealed class Wave1TalentPoolRiasecShownBunitTests : BunitContext
{
    public Wave1TalentPoolRiasecShownBunitTests()
    {
        Wave1TalentPoolRiasecBunitSupport.Register(this,
            services => services.AddSingleton(Wave1TalentPoolRiasecBunitSupport.BuildConfiguration(showRiasec: true)));
    }

    [Fact]
    public void Talent_pool_tag_input_shows_riasec_when_config_on()
    {
        var cut = Render<TalentPoolSection>();
        cut.WaitForAssertion(() => Assert.Contains("Zoeken", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("RIASEC", cut.Markup, StringComparison.Ordinal);
    }
}

file static class Wave1TalentPoolRiasecBunitSupport
{
    internal static IConfiguration BuildConfiguration(bool showRiasec)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [Jobsy.Core.Rules.TalentPoolRiasecVisibility.ConfigKey] = showRiasec ? "true" : "false"
            })
            .Build();

    internal static void Register(BunitContext ctx, Action<IServiceCollection> extra)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        extra(ctx.Services);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(new TalentPoolFakeAuth());
        ctx.Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        ctx.Services.AddSingleton<IGeocodingClient>(new TalentPoolNullGeo());
        ctx.Services.AddSingleton(new HttpClient(new TalentPoolApiStub()) { BaseAddress = new Uri("http://localhost/") });
        ctx.Services.AddSingleton(sp => new JobsyApiClient(sp.GetRequiredService<HttpClient>()));
        ctx.Services.AddSingleton<IFeatureFlags>(new TalentPoolFlags());
        BunitAntiforgery.Register(ctx.Services);
    }

    private sealed class TalentPoolFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature is PlatformFeature.Employers);

        public void Invalidate()
        {
        }
    }

    private sealed class TalentPoolFakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "bm"), new Claim(ClaimTypes.Role, "BranchManager")],
                    "t"))));
    }

    private sealed class TalentPoolNullGeo : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class TalentPoolApiStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
