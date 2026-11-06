using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class R9cPaspoortBewaardGuardTests
{
    [Fact]
    public void Timmerman_career_plan_uses_trade_specific_steps()
    {
        var plan = HorizonCareerPathBuilder.BuildLocal("Timmerman");
        Assert.Equal("Timmerman", plan.DreamTitle);
        Assert.Contains(plan.Steps, s => s.Title.Contains("MBO Timmeren", StringComparison.Ordinal));
        Assert.Contains(plan.Steps, s => s.Title.Contains("BBL", StringComparison.Ordinal) || s.Summary.Contains("leerbedrijf", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(plan.Steps, s => s.Title.Contains("VCA", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.Steps, s => s.Title.Contains("Profiel als basis", StringComparison.Ordinal));
    }

    [Fact]
    public void Passport_tabs_script_strips_check_and_prevents_full_navigation()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/passport-ssr-tabs.js"));
        Assert.Contains("TAB_ONLY_PARAMS", js, StringComparison.Ordinal);
        Assert.Contains("\"check\"", js, StringComparison.Ordinal);
        Assert.Contains("e.preventDefault()", js, StringComparison.Ordinal);
        Assert.Contains("bindPaspoortTabLinks", js, StringComparison.Ordinal);
        Assert.Contains("stripPassportQueryNoise", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_proof_and_data_ctas_use_tab_links_without_enhance_nav()
    {
        var root = TestRepo.FindRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrShells.cs"));
        var fit = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrFitHtml.cs"));
        Assert.Contains("data-passport-tab-link=\\\"proof\\\"", shells, StringComparison.Ordinal);
        Assert.Contains("data-passport-tab-link=\\\"data\\\"", fit, StringComparison.Ordinal);
        Assert.Contains("data-enhance-nav=\\\"false\\\"", shells, StringComparison.Ordinal);
    }

    [Fact]
    public void Saved_mobile_toast_raises_above_coach_dock()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/saved-ssr-unsave.js"));
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/kandidaat-banen.css"));
        Assert.Contains("kb-saved-toast-open", js, StringComparison.Ordinal);
        Assert.Contains("kb-saved-toast-open", css, StringComparison.Ordinal);
        Assert.Contains("z-index: 10090", css, StringComparison.Ordinal);
        Assert.Contains("document.querySelector(\"[data-saved-toast]\")", js, StringComparison.Ordinal);
        Assert.DoesNotContain(".saved-page-wrap [data-saved-toast]", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Activiteiten_seed_repair_purges_stale_uitkomst_copy()
    {
        var root = TestRepo.FindRoot();
        var repair = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Ops/JobSearchActivityAcceptatieRepair.cs"));
        var seeder = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Ops/TestAccountsSollicitatiesDemoSeeder.cs"));
        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/JobSearchActivityService.cs"));
        Assert.Contains("PurgeStaleOutcomeCopyAsync", repair, StringComparison.Ordinal);
        Assert.Contains("PurgeStaleOutcomeCopyAsync", seeder, StringComparison.Ordinal);
        Assert.Contains("ClearOutcomeCopy", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Sollic_list_card_hint_uses_after_match_timeline_ssot()
    {
        var root = TestRepo.FindRoot();
        var data = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Applications/ApplicationsSsrData.cs"));
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Applications/ApplicationsSsrShells.cs"));
        Assert.Contains("FormatListCardTimelineSummary", data, StringComparison.Ordinal);
        Assert.Contains("row.CardHint", shells, StringComparison.Ordinal);
        Assert.DoesNotContain("AfterMatch is null && !string.IsNullOrWhiteSpace(row.CardHint)", shells, StringComparison.Ordinal);
    }
}
