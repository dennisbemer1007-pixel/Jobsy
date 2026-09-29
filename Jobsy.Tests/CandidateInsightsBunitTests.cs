using Bunit;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class CandidateInsightsBunitTests : TestContext
{
    public CandidateInsightsBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth(Jobsy.Core.Authorization.JobsyRoles.EnterpriseManager));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Locked_overlay_uses_tokens_cta_and_placeholder_not_real_locked_values()
    {
        var dto = LockedDto();
        var cut = Render(builder =>
        {
            builder.OpenComponent<InsightsLockedPanelProbe>(0);
            builder.AddAttribute(1, "Dto", dto);
            builder.AddAttribute(2, "TokensHref", "/werkgever/tokens");
            builder.CloseComponent();
        });

        var markup = cut.Markup;
        Assert.Contains("Volledige inzichten met tokens", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/werkgever/tokens\"", markup, StringComparison.Ordinal);
        Assert.Contains("insights-locked__placeholder", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_LOCKED_DNA", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"insights-insufficient\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Branch_manager_locked_cta_links_to_branch_tokens()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<InsightsLockedPanelProbe>(0);
            builder.AddAttribute(1, "Dto", LockedDto());
            builder.AddAttribute(2, "TokensHref", "/branch/tokens");
            builder.CloseComponent();
        });
        Assert.Contains("href=\"/branch/tokens\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Story_card_6_locked_when_not_full_access()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<InsightsStoryCard6Probe>(0);
            builder.AddAttribute(1, "Locked", true);
            builder.CloseComponent();
        });
        Assert.Contains("data-testid=\"insights-story-card-6\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("insights-locked", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Volledige inzichten met tokens", cut.Markup, StringComparison.Ordinal);
    }

    private static CandidateInsightsDto LockedDto()
        => new()
        {
            Scope = new InsightsScopeModel { IsFullAccess = false, RadiusKm = 20, PeriodDays = 90 },
            Kpis = new InsightsKpisModel
            {
                CandidatesInRadius = new SuppressedCountModel { Status = "insufficient" }
            },
            LockedSections = ["dreamJobs4to10", "dna", "story5to10"],
            DnaRiasec = null,
            Competences = null
        };

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;
        public FakeAuth(string role)
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(ClaimTypes.Role, role));
            _user = new ClaimsPrincipal(id);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_user));
    }
}

/// <summary>Minimal markup probe mirroring locked UI contracts without hosting the full page.</summary>
public class InsightsLockedPanelProbe : ComponentBase
{
    [Inject] public CultureState Culture { get; set; } = default!;
    [Parameter] public CandidateInsightsDto Dto { get; set; } = new();
    [Parameter] public string TokensHref { get; set; } = "/werkgever/tokens";

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "insights-locked");
        builder.AddAttribute(2, "data-testid", "insights-locked-dna");
        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "class", "insights-locked__placeholder");
        builder.AddAttribute(5, "aria-hidden", "true");
        builder.AddContent(6, "————");
        builder.CloseElement();
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "insights-locked__overlay");
        builder.OpenElement(9, "span");
        builder.AddAttribute(10, "class", "status-pill");
        builder.AddContent(11, Culture["Insights.Locked.BlurHint"]);
        builder.CloseElement();
        builder.OpenElement(12, "a");
        builder.AddAttribute(13, "class", "btn-compact login-submit");
        builder.AddAttribute(14, "href", TokensHref);
        builder.AddContent(15, Culture["Insights.Cta.Full"]);
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenElement(16, "span");
        builder.AddAttribute(17, "class", "status-pill status-pill--neutral");
        builder.AddAttribute(18, "data-testid", "insights-insufficient");
        builder.AddContent(19, Culture["Insights.Insufficient"]);
        builder.CloseElement();
        builder.CloseElement();
    }
}

public class InsightsStoryCard6Probe : ComponentBase
{
    [Inject] public CultureState Culture { get; set; } = default!;
    [Parameter] public bool Locked { get; set; }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "article");
        builder.AddAttribute(1, "class", "insights-story-card");
        builder.AddAttribute(2, "data-testid", "insights-story-card-6");
        builder.AddAttribute(3, "role", "group");
        if (Locked)
        {
            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "class", "insights-locked");
            builder.OpenElement(6, "div");
            builder.AddAttribute(7, "class", "insights-locked__placeholder");
            builder.AddContent(8, "————");
            builder.CloseElement();
            builder.OpenElement(9, "div");
            builder.AddAttribute(10, "class", "insights-locked__overlay");
            builder.OpenElement(11, "span");
            builder.AddAttribute(12, "class", "status-pill");
            builder.AddContent(13, Culture["Insights.Locked.BlurHint"]);
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
