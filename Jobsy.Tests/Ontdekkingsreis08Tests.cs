using Bunit;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class Ontdekkingsreis08CatalogTests
{
    [Fact]
    public void Deeper_sets_are_valid_duplicate_free_and_sized()
    {
        AssertSet(OnboardingWizardCatalog.CompetencyDeeperQuestionIds, CompetencyTestCatalog.QuestionCount);
        AssertSet(OnboardingWizardCatalog.CareerDeeperQuestionIds, CareerTestCatalog.QuestionCount);
        AssertSet(OnboardingWizardCatalog.CultureDeeperQuestionIds, CulturePersonalityCatalog.QuestionCount);
        AssertSet(OnboardingWizardCatalog.ValuesDeeperQuestionIds, SchwartzValuesCatalog.QuestionCount);
    }

    [Fact]
    public void Career_deeper_set_includes_Artistic()
    {
        var artisticIds = CareerTestCatalog.Questions
            .Where(q => q.Category == CareerTestCatalog.Artistic)
            .Select(q => q.Id)
            .ToHashSet();
        Assert.Contains(OnboardingWizardCatalog.CareerDeeperQuestionIds, id => artisticIds.Contains(id));
        Assert.Contains(10, OnboardingWizardCatalog.CareerDeeperQuestionIds);
    }

    [Fact]
    public void Full_counts_match_catalogs()
    {
        Assert.Equal(25, OnboardingWizardCatalog.FullLevelCount(OnboardingWizardCatalog.OnboardingTestKind.Competency));
        Assert.Equal(25, OnboardingWizardCatalog.FullLevelCount(OnboardingWizardCatalog.OnboardingTestKind.Career));
        Assert.Equal(18, OnboardingWizardCatalog.FullLevelCount(OnboardingWizardCatalog.OnboardingTestKind.Culture));
        Assert.Equal(25, OnboardingWizardCatalog.FullLevelCount(OnboardingWizardCatalog.OnboardingTestKind.Values));
    }

    [Fact]
    public void Reached_level_from_answered_counts()
    {
        var kind = OnboardingWizardCatalog.OnboardingTestKind.Competency;
        Assert.Equal(0, OnboardingWizardCatalog.ReachedLevel(3, kind));
        Assert.Equal(5, OnboardingWizardCatalog.ReachedLevel(5, kind));
        Assert.Equal(5, OnboardingWizardCatalog.ReachedLevel(9, kind));
        Assert.Equal(10, OnboardingWizardCatalog.ReachedLevel(12, kind));
        Assert.Equal(25, OnboardingWizardCatalog.ReachedLevel(25, kind));
        Assert.Equal(18, OnboardingWizardCatalog.ReachedLevel(18, OnboardingWizardCatalog.OnboardingTestKind.Culture));
    }

    [Fact]
    public void QuestionIdsUpTo_grows_without_duplicates()
    {
        var kind = OnboardingWizardCatalog.OnboardingTestKind.Career;
        var mini = OnboardingWizardCatalog.QuestionIdsUpTo(kind, 5);
        var deeper = OnboardingWizardCatalog.QuestionIdsUpTo(kind, 10);
        var full = OnboardingWizardCatalog.QuestionIdsUpTo(kind, 25);
        Assert.Equal(5, mini.Length);
        Assert.Equal(10, deeper.Length);
        Assert.Equal(25, full.Length);
        Assert.Equal(mini.Length, mini.Distinct().Count());
        Assert.Equal(deeper.Length, deeper.Distinct().Count());
        Assert.True(mini.All(deeper.Contains));
        Assert.Contains(10, deeper); // Artistic
    }

    [Fact]
    public void Depth_options_disable_already_reached_levels()
    {
        var opts = JourneyTestFlow.BuildDepthOptions(OnboardingWizardCatalog.OnboardingTestKind.Culture, 12);
        Assert.True(opts[0].Disabled); // 5
        Assert.True(opts[1].Disabled); // 10
        Assert.False(opts[2].Disabled); // 18
        Assert.Equal("Discovery.Shed.DeepestSubCulture", opts[2].SubKey);
    }

    private static void AssertSet(int[] ids, int max)
    {
        Assert.Equal(5, ids.Length);
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.All(ids, id => Assert.InRange(id, 1, max));
    }
}

public class Ontdekkingsreis08RoutesNavTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void StartPath_flag_aware()
    {
        Assert.Equal("/candidate/ontdekkingsreis",
            OnboardingRoutes.StartPath(new FeatureFlagSnapshot(true, true)));
        Assert.Equal("/candidate/start",
            OnboardingRoutes.StartPath(new FeatureFlagSnapshot(true, false)));
    }

    [Fact]
    public void Entry_points_use_OnboardingRoutes_StartPath()
    {
        var files = new[]
        {
            "Jobsy.Web/Auth/AuthRedirects.cs",
            "Jobsy.Web/Components/Shared/CandidateOnboardingResumeCard.razor",
            "Jobsy.Web/Components/Match/MatchUnlockPanel.razor",
            "Jobsy.Web/Components/Pages/Public/GratisDna.razor"
        };
        foreach (var rel in files)
        {
            var src = File.ReadAllText(Path.Combine(RepoRoot, rel));
            Assert.Contains("OnboardingRoutes.StartPath", src, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Hardcoded_candidate_start_only_in_wizard_and_routes()
    {
        var root = Path.Combine(RepoRoot, "Jobsy.Web");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.{cs,razor}", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                     .Concat(Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
                     .Distinct())
        {
            var rel = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            if (rel is "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"
                or "Jobsy.Web/Navigation/OnboardingRoutes.cs"
                or "Jobsy.Web/Auth/AuthRedirects.cs"
                or "Jobsy.Web/Navigation/RoleNavCatalog.cs")
            {
                continue;
            }

            // DiscoveryJourney fallback attribute still points at classic start when flag OFF.
            if (rel.Contains("DiscoveryJourney.razor", StringComparison.Ordinal)) continue;

            var src = File.ReadAllText(file);
            if (src.Contains("\"/candidate/start\"", StringComparison.Ordinal)
                || src.Contains("'/candidate/start'", StringComparison.Ordinal))
            {
                // Allow ClassicStartPath constant usages via OnboardingRoutes
                if (src.Contains("OnboardingRoutes.ClassicStartPath", StringComparison.Ordinal)
                    && !src.Contains("\"/candidate/start\"", StringComparison.Ordinal))
                {
                    continue;
                }

                if (src.Contains("\"/candidate/start\"", StringComparison.Ordinal))
                {
                    offenders.Add(rel);
                }
            }
        }

        Assert.True(offenders.Count == 0, "Hardcoded /candidate/start in: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Discovery_nav_item_has_short_title_and_compass()
    {
        Assert.Equal("Nav.Discovery.Short", RoleNavCatalog.DiscoveryItem.ShortTitleKey);
        Assert.Equal("Nav.Passport.Short", RoleNavCatalog.PassportItem.ShortTitleKey);
        Assert.Contains("circle", RoleNavCatalog.DiscoveryItem.Svg, StringComparison.Ordinal);
        Assert.Contains("/candidate/start", RoleNavCatalog.DiscoveryItem.ExtraActivePaths ?? []);
    }

    [Fact]
    public void Focus_mode_only_with_stap_query()
    {
        Assert.False(MainLayout.IsQuestionnairePath("candidate/ontdekkingsreis", "candidate/ontdekkingsreis"));
        Assert.True(MainLayout.IsQuestionnairePath("candidate/ontdekkingsreis", "candidate/ontdekkingsreis?stap=7"));
        Assert.True(MainLayout.IsQuestionnairePath("candidate/ontdekkingsreis", "candidate/ontdekkingsreis?stap=klaar"));
        Assert.True(MainLayout.IsQuestionnairePath("candidate/start", "candidate/start"));
    }

    [Fact]
    public void Discovery_strings_cover_08_keys()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsDiscovery.MergeAll(nl, en, pl, ro, ar);
        UiStringsPassport.MergeAll(nl, en, pl, ro, ar);

        foreach (var key in new[]
                 {
                     "Discovery.Test.Competency.Title", "Discovery.Shed.Title", "Discovery.End.Title",
                     "Discovery.Overview.Title", "Nav.Discovery", "Nav.Discovery.Short", "Nav.Passport.Short",
                     "MatchUnlock.ContinueStart", "Discovery.End.LeadEmployersOff", "Discovery.Shed.DeepestSubCulture"
                 })
        {
            Assert.True(nl.ContainsKey(key), key);
            Assert.False(string.Equals(nl[key], en[key], StringComparison.Ordinal), key);
        }
    }
}

public class Ontdekkingsreis08ShedBunitTests : BunitContext
{
    public Ontdekkingsreis08ShedBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Shed_shows_three_options_with_culture_alle_18_and_default_keep()
    {
        var opts = JourneyTestFlow.BuildDepthOptions(OnboardingWizardCatalog.OnboardingTestKind.Culture, 5)
            .Select(o => new JourneyShedMoment.DepthOption(o.Level, o.TitleKey, o.SubKey, o.Dots, o.Disabled))
            .ToList();
        var cut = Render(builder =>
        {
            builder.OpenComponent<JourneyShedMoment>(0);
            builder.AddAttribute(1, "Step", 9);
            builder.AddAttribute(2, "TestName", "Cultuur");
            builder.AddAttribute(3, "ImpressionLines", (IReadOnlyList<string>)["Autonomie (voorlopig)"]);
            builder.AddAttribute(4, "TestTabs", (IReadOnlyList<JourneyShedMoment.TestTab>)
            [
                new(7, "Onboarding.Test.Competency"),
                new(8, "Onboarding.Test.Career"),
                new(9, "Onboarding.Test.Culture"),
                new(10, "Onboarding.Test.Values")
            ]);
            builder.AddAttribute(5, "Options", (IReadOnlyList<JourneyShedMoment.DepthOption>)opts);
            builder.AddAttribute(6, "SelectedLevel", 5);
            builder.AddAttribute(7, "PrimaryLabel", "Duik dieper");
            builder.CloseComponent();
        });

        Assert.Contains("Zo laten", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Iets dieper", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Heel diep", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("alle 18", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Laag 9 van 10 eraf", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Duik dieper", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shed_reduced_motion_uses_static_class()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<JourneyShedMoment>(0);
            builder.AddAttribute(1, "Step", 7);
            builder.AddAttribute(2, "TestName", "Competenties");
            builder.AddAttribute(3, "ImpressionLines", (IReadOnlyList<string>)[]);
            builder.AddAttribute(4, "TestTabs", (IReadOnlyList<JourneyShedMoment.TestTab>)[]);
            builder.AddAttribute(5, "Options", (IReadOnlyList<JourneyShedMoment.DepthOption>)[]);
            builder.AddAttribute(6, "PreferReducedMotion", true);
            builder.AddAttribute(7, "PrimaryLabel", "Verder");
            builder.CloseComponent();
        });

        Assert.Contains("is-static", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("is-reduced-motion", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void End_facts_and_employers_off_variants()
    {
        var impression = new OnboardingImpression
        {
            Strengths = [new() { Label = "Zorgzaam", Sentence = "Je werkt graag samen" }],
            Riasec = [new() { Label = "Helpen" }, new() { Label = "Maken" }],
            TopValue = new() { Label = "Verbinding" },
            MatchingVacancyCount = 12
        };
        var facts = JourneyTestFlow.EndFacts(impression);
        Assert.Equal("Zorgzaam", facts.Strength);
        Assert.Contains("Helpen", facts.Work);
        Assert.Equal("Verbinding", facts.Value);
        Assert.Null(JourneyTestFlow.EndFacts(null).Strength);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}

public class Ontdekkingsreis08SourceTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Journey_wires_shed_end_overview_and_complete()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor"));
        Assert.Contains("JourneyShedMoment", src, StringComparison.Ordinal);
        Assert.Contains("JourneyEndScreen", src, StringComparison.Ordinal);
        Assert.Contains("JourneyOverview", src, StringComparison.Ordinal);
        Assert.Contains("CompleteMyOnboardingAsync", src, StringComparison.Ordinal);
        Assert.Contains("ShowEndAsync", src, StringComparison.Ordinal);
        Assert.Contains("DeclineConsentAndFinishAsync", src, StringComparison.Ordinal);
        Assert.Contains("OnboardingInstallPrompt",
            File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Candidate/Discovery/JourneyEndScreen.razor")),
            StringComparison.Ordinal);
        Assert.DoesNotContain("CompleteJourneyTransitionalAsync", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Impression_empties_matches_when_employers_off()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot,
            "Jobsy.Infrastructure/Services/CandidateOnboardingService.cs"));
        Assert.Contains("PlatformFeature.Employers", src, StringComparison.Ordinal);
        Assert.Contains("matchCount = 0", src, StringComparison.Ordinal);
    }
}
