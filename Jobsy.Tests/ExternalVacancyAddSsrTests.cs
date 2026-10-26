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
    public void Detail_page_omits_unknown_fact_rows()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var detail = new ExternalVacancyDetailDto(
            Guid.Parse("96d18a1b-0000-4000-8000-000000000003"),
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
            new ExternalVacancyMatchInsightsDto([], []),
            null,
            CandidateExternalVacancyStatus.Saved,
            DateTime.UtcNow,
            true,
            [],
            false,
            false);

        var html = ExternalVacancyDetailSsrShells.BuildBody(culture, detail);
        Assert.Contains("BV", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Dat weet ik niet", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Detail_page_hides_match_block_when_only_placeholders()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var detail = new ExternalVacancyDetailDto(
            Guid.Parse("96d18a1b-0000-4000-8000-000000000002"),
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
            new ExternalVacancyMatchInsightsDto(
                ["Competenties beschikbaar", "Cultuurprofiel beschikbaar"],
                ["Dat weet ik niet"]),
            null,
            CandidateExternalVacancyStatus.Saved,
            DateTime.UtcNow,
            true,
            [],
            false,
            false);

        var html = ExternalVacancyDetailSsrShells.BuildBody(culture, detail);
        Assert.DoesNotContain("Hier scoor je goed op", html, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-ext__match-panel--good", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Detail_page_hides_generic_culture_unavailable_challenge()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var detail = new ExternalVacancyDetailDto(
            Guid.Parse("96d18a1b-0000-4000-8000-000000000004"),
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
            new ExternalVacancyMatchInsightsDto(
                ["Echte kracht"],
                ["Cultuurprofiel niet beschikbaar"]),
            null,
            CandidateExternalVacancyStatus.Saved,
            DateTime.UtcNow,
            true,
            [],
            false,
            false);

        var html = ExternalVacancyDetailSsrShells.BuildBody(culture, detail);
        Assert.Contains("Echte kracht", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Cultuurprofiel niet beschikbaar", html, StringComparison.Ordinal);
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
