using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class ExternalVacancyAddSsrTests : BunitContext
{
    public ExternalVacancyAddSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    [Fact]
    public void Detail_page_shell_renders_facts_and_apply_link()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var detail = new Jobsy.Core.Contracts.ExternalVacancyDetailDto(
            Guid.Parse("96d18a1b-0000-4000-8000-000000000001"),
            "https://example.com/job",
            "Test extern",
            "BV",
            "Den Haag",
            "",
            "",
            "",
            "",
            [],
            new Dictionary<string, string> { ["bedrijf"] = "BV" },
            new Jobsy.Core.Contracts.ExternalVacancyMatchInsightsDto(["Sterk"], ["Gap"]),
            null,
            Jobsy.Core.Enums.CandidateExternalVacancyStatus.Saved,
            DateTime.UtcNow,
            true,
            [],
            false,
            false);

        var html = ExternalVacancyDetailSsrShells.BuildBody(culture, detail);
        Assert.Contains("data-testid=\"kb-ext-detail\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-ext-apply-cta\"", html, StringComparison.Ordinal);
        Assert.Contains("Sterk", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Add_page_shell_includes_url_field_and_submit()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var html = ExternalVacancyAddSsrShells.BuildBody(culture, "https://example.com/job");

        Assert.Contains("data-testid=\"kb-ext-add\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-ext-url-input\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-ext-add-submit\"", html, StringComparison.Ordinal);
        Assert.Contains("https://example.com/job", html, StringComparison.Ordinal);
    }
}
