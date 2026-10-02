using Bunit;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class CandidateInsightsBunitTests : BunitContext
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
    public void Locked_card_uses_gold_pattern_without_real_locked_values()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<InsightsLockedPanelProbe>(0);
            builder.AddAttribute(1, "Title", "Werkvelden");
            builder.CloseComponent();
        });

        var markup = cut.Markup;
        Assert.Contains("test-result-card-locked", markup, StringComparison.Ordinal);
        Assert.Contains("test-result-lock-chip", markup, StringComparison.Ordinal);
        Assert.Contains("Vergrendeld", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_LOCKED_DNA", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"insights-locked-probe\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Locked_kpi_renders_no_numeric_value()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<InsightsKpiLockedProbe>(0);
            builder.CloseComponent();
        });
        Assert.Contains("wg-kpi-locked", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Premium", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("2340", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("test-result-card-locked", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Vergrendeld", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Rm_premium_shows_ask_bm_text_without_unlock_button()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<InsightsPremiumProbe>(0);
            builder.AddAttribute(1, "IsRegionalManager", true);
            builder.CloseComponent();
        });
        Assert.Contains("bedrijfsmanager", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("btn-gold", cut.Markup, StringComparison.Ordinal);
    }

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

public class InsightsLockedPanelProbe : ComponentBase
{
    [Inject] public CultureState Culture { get; set; } = default!;
    [Parameter] public string Title { get; set; } = "Werkvelden";

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<Jobsy.Web.Components.Werkgever.Insights.WgLockedCard>(0);
        builder.AddAttribute(1, "Title", Title);
        builder.AddAttribute(2, "TestId", "insights-locked-probe");
        builder.CloseComponent();
    }
}

public class InsightsKpiLockedProbe : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<Jobsy.Web.Components.Werkgever.Insights.WgInsightsKpiLocked>(0);
        builder.AddAttribute(1, "Title", "Passend bij vacatures");
        builder.AddAttribute(2, "Question", "Hoeveel passen?");
        builder.AddAttribute(3, "TestId", "insights-kpi-locked-probe");
        builder.CloseComponent();
    }
}

public class InsightsPremiumProbe : ComponentBase
{
    [Parameter] public bool IsRegionalManager { get; set; }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<Jobsy.Web.Components.Werkgever.Insights.WgInsightsPremium>(0);
        builder.AddAttribute(1, "ScopeLabel", "alle vestigingen");
        builder.AddAttribute(2, "PriceLabel", "12 tokens");
        builder.AddAttribute(3, "MetaLabel", "90 dagen");
        builder.AddAttribute(4, "IsRegionalManager", IsRegionalManager);
        builder.AddAttribute(5, "CanUnlock", true);
        builder.CloseComponent();
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
        if (Locked)
        {
            builder.OpenComponent<Jobsy.Web.Components.Werkgever.Insights.WgLockedCard>(3);
            builder.AddAttribute(4, "Title", Culture["Insights.Section.Vacancies"]);
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(5, "open");
        }

        builder.CloseElement();
    }
}
