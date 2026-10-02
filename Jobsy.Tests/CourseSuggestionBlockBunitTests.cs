using System.Security.Claims;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class CourseSuggestionBlockBunitTests : BunitContext
{
    public CourseSuggestionBlockBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new FixedFlags());
        var http = new HttpClient(new FakeHandler()) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new Jobsy.Web.Services.JobsyApiClient(http));
        Services.AddSingleton(sp => new Jobsy.Web.Services.UserFacingError(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Jobsy.Web.Services.UserFacingError>.Instance,
            sp.GetRequiredService<CultureState>()));
    }

    [Fact]
    public void Empty_slots_render_nothing()
    {
        var cut = Render<CourseSuggestionBlock>(p => p
            .Add(x => x.Slots, Array.Empty<PassportCourseCard>())
            .Add(x => x.SkillLabel, "Plannen"));
        Assert.DoesNotContain("passport-course", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Gratis", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Free_first_partner_second_and_disclosure_note()
    {
        var slots = new List<PassportCourseCard>
        {
            new()
            {
                OfferId = Guid.NewGuid(),
                Title = "Slim plannen",
                ProviderName = "LeerPlein",
                Type = "Workshop",
                DurationValue = 2,
                DurationUnit = "Hours",
                Delivery = "Online",
                IsFree = true,
                Rel = "noopener"
            },
            new()
            {
                OfferId = Guid.NewGuid(),
                Title = "Plannen in de zorg",
                ProviderName = "ZorgStart",
                Type = "Cursus",
                DurationValue = 4,
                DurationUnit = "Weeks",
                Delivery = "OnSite",
                Location = "Den Haag",
                IsPartner = true,
                Rel = "sponsored noopener noreferrer"
            }
        };

        var cut = Render<CourseSuggestionBlock>(p => p
            .Add(x => x.Slots, slots)
            .Add(x => x.SkillLabel, "Plannen"));

        Assert.Contains("Gratis", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Partnerlink", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Lobsy kan een vergoeding krijgen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Slim plannen", cut.Markup, StringComparison.Ordinal);
        var freeIdx = cut.Markup.IndexOf("Slim plannen", StringComparison.Ordinal);
        var partnerIdx = cut.Markup.IndexOf("Plannen in de zorg", StringComparison.Ordinal);
        Assert.True(freeIdx >= 0 && partnerIdx > freeIdx);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "t"), new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }

    private sealed class FixedFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate()
        {
        }
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
    }
}
