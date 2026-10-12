using System.Text.Json;
using Jobsy.Api.Security;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

/// <summary>Regression guards for anti-scraping / IP-protection hardening.</summary>
public sealed class AntiScrapingAuditTests
{
    [Fact]
    public void Test_question_dtos_do_not_serialize_reverse_scoring_keys()
    {
        var competency = new CompetencyQuestionDto(1, CompetencyTestCatalog.Samenwerken, Reverse: true, "k", IsRiasec: false);
        var career = new CareerQuestionDto(1, CareerTestCatalog.Realistic, Reverse: true, "k");
        var deep = new DeepAnalysisQuestionDto(1, "f", "d", Reverse: true, "prompt");

        Assert.DoesNotContain("reverse", JsonSerializer.Serialize(competency), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reverse", JsonSerializer.Serialize(career), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reverse", JsonSerializer.Serialize(deep), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Anonymous_reference_endpoints_use_public_read_rate_limiting()
    {
        var root = RepoRoot.Find();
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/WagesController.cs")));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/MasterdataController.cs")));
        Assert.Contains("Take(PublicApiLimits.MaxMasterdataOptions)", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/MasterdataController.cs")));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/VacancyCategoriesController.cs")));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/ExclusivitySettingsController.cs")));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/RegionHostsController.cs")));
    }

    [Fact]
    public void Site_status_and_settings_bootstrap_are_rate_limited()
    {
        var root = RepoRoot.Find();
        var site = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/SiteController.cs"));
        Assert.Contains("Get(\"status\")", site, StringComparison.Ordinal);
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", site);

        var settings = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/SettingsController.cs"));
        Assert.Contains("GetFeatureFlags", settings, StringComparison.Ordinal);
        Assert.Contains("GetSessionSecurity", settings, StringComparison.Ordinal);
        Assert.Matches(
            @"GetFeatureFlags[\s\S]*?\[EnableRateLimiting\(""public-read""\)\]",
            settings);
    }

    [Fact]
    public void Candidate_test_reads_are_rate_limited()
    {
        var root = RepoRoot.Find();
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/CandidateCompetenciesController.cs")));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/CandidateCareerInterestsController.cs")));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/DeepAnalysisController.cs")));
    }

    [Fact]
    public void Blazor_host_is_interactive_server_not_wasm()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Program.cs"));
        Assert.Contains("AddInteractiveServerComponents", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddInteractiveWebAssemblyComponents", program, StringComparison.Ordinal);
        var webProj = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Jobsy.Web.csproj"));
        Assert.DoesNotContain("Jobsy.Infrastructure", webProj, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_api_limits_masterdata_cap_is_reasonable()
    {
        Assert.InRange(PublicApiLimits.MaxMasterdataOptions, 100, 2_000);
    }
}
