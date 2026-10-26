using System.Security.Claims;
using Bunit;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Saved;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 05: Bewaard SSR markup (rows + desktop cards).
/// </summary>
public class KbSavedListBunitTests : BunitContext
{
    public KbSavedListBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeCandidateAuth());
        Services.AddSingleton(sp =>
        {
            var culture = new CultureState(
                sp.GetRequiredService<IJSRuntime>(),
                sp,
                sp.GetRequiredService<AuthenticationStateProvider>());
            culture.InitializeFromLanguage("nl");
            return culture;
        });
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Saved_row_renders_thumb_title_company_fit_status_and_heart()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var html = Build(
        [
            Item(id, "Schoonmaker kantoren", "CleanWest", "Rijswijk",
                fit: 61, band: "Good", state: nameof(KbSavedJobStateKind.Open))
        ]);

        Assert.Contains("candidate-job-list-tabs", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/candidate/applications\"", html, StringComparison.Ordinal);
        Assert.Contains("kb-saved-row", html, StringComparison.Ordinal);
        Assert.Contains("Schoonmaker kantoren", html, StringComparison.Ordinal);
        Assert.Contains("CleanWest", html, StringComparison.Ordinal);
        Assert.Contains("Rijswijk", html, StringComparison.Ordinal);
        Assert.Contains("kb-saved-row__fit", html, StringComparison.Ordinal);
        Assert.Contains("saved-unsave-", html, StringComparison.Ordinal);
        Assert.Contains("Uit Bewaard halen", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Title_link_targets_vacancy_and_closed_targets_similar()
    {
        var openId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var closedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var category = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var html = Build(
        [
            Item(openId, "Open baan", "Acme", "Delft",
                fit: 80, band: "Strong", state: nameof(KbSavedJobStateKind.Open)),
            Item(closedId, "Gesloten baan", "Beta", "Den Haag",
                fit: 50, band: "Some", state: nameof(KbSavedJobStateKind.Closed), categoryId: category),
        ]);

        Assert.Contains($"/vacancies/{openId}", html, StringComparison.Ordinal);
        Assert.Contains($"{KbRoutes.Map}?categorie={category:D}", html, StringComparison.Ordinal);
        Assert.Contains("kb-saved-row--closed", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_closed_shows_paspoort_link_instead_of_percent()
    {
        var html = Build(
        [
            Item(Guid.NewGuid(), "Zonder fit", "Co", "Plaats",
                fit: null, band: null, state: nameof(KbSavedJobStateKind.Open), gateClosed: true)
        ]);

        Assert.Contains("Maak je paspoort af", html, StringComparison.Ordinal);
        Assert.Contains(KbRoutes.PaspoortTests, html, StringComparison.Ordinal);
        Assert.DoesNotContain("%", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Sort_links_and_count_label_are_present()
    {
        var html = Build(
        [
            Item(Guid.NewGuid(), "Titel", "Firma", "Stad",
                fit: 74, band: "Good", state: nameof(KbSavedJobStateKind.Open))
        ]);

        Assert.Contains("data-saved-sort", html, StringComparison.Ordinal);
        Assert.Contains("data-saved-filter", html, StringComparison.Ordinal);
        Assert.Contains("kb-saved-card__media-link", html, StringComparison.Ordinal);
    }

    private string Build(IReadOnlyList<CandidateEngagementItem> items)
    {
        var data = new SavedSsrData.PageData(
            true,
            true,
            true,
            SavedSsrQuery.FilterAll,
            SavedSsrQuery.SortNewest,
            null,
            false,
            items,
            items,
            items.Count(i => !SavedSsrData.IsClosed(i)),
            items.Count(SavedSsrData.IsClosed),
            [],
            false);
        return SavedSsrShells.BuildBody(Culture, data, "token", new DefaultHttpContext());
    }

    private sealed class FakeCandidateAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Candidate"), new Claim(ClaimTypes.Name, "test")],
                "test");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    private static CandidateEngagementItem Item(
        Guid vacancyId,
        string title,
        string company,
        string place,
        int? fit,
        string? band,
        string state,
        bool gateClosed = false,
        Guid? categoryId = null,
        bool applied = false,
        DateTime? createdAt = null)
        => new()
        {
            VacancyId = vacancyId,
            VacancyTitle = title,
            CompanyName = company,
            LocationLabel = place,
            FitPercent = fit,
            FitBand = band,
            SavedStateKind = state,
            FitGateClosed = gateClosed,
            CategoryId = categoryId,
            HasApplied = applied,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };
}
