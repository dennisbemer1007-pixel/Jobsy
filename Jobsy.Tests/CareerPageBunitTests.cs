using Bunit;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Components.Candidate.Career;
using Jobsy.Web.Components.Candidate.Journey;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class CareerPageBunitTests : BunitContext
{
    public CareerPageBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<CareerPathService>();
    }

    // ---------- empty state ----------

    [Fact]
    public void Empty_state_shows_one_h1_and_a_radiogroup_of_suggestions()
    {
        var cut = Render<CareerEmptyCard>(p => p
            .Add(x => x.Suggestions, Suggestions(3)));

        Assert.Equal(1, Occurrences(cut.Markup, "<h1"));
        Assert.Contains("Waar wil jij naartoe groeien?", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(3, cut.FindAll("[role=\"radio\"]").Count);
        Assert.DoesNotContain("Stip op de horizon", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_state_without_suggestions_starts_with_the_search_field()
    {
        var cut = Render<CareerEmptyCard>(p => p
            .Add(x => x.Suggestions, Suggestions(0)));

        Assert.DoesNotContain("role=\"radiogroup\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("role=\"combobox\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Primary_stays_disabled_until_a_dream_is_chosen()
    {
        var cut = Render<CareerEmptyCard>(p => p
            .Add(x => x.Suggestions, Suggestions(2))
            .Add(x => x.HasChoice, false));

        Assert.True(cut.Find(".career-btn--primary").HasAttribute("disabled"));

        cut.Render(p => p.Add(x => x.HasChoice, true));
        Assert.False(cut.Find(".career-btn--primary").HasAttribute("disabled"));
    }

    [Fact]
    public void Picking_a_suggestion_reports_the_catalog_key()
    {
        CareerDreamChoice? chosen = null;
        var cut = Render<CareerDreamPicker>(p => p
            .Add(x => x.Suggestions, Suggestions(2))
            .Add(x => x.OnChoice, c => chosen = c));

        cut.Find("[role=\"radio\"]").Click();

        Assert.NotNull(chosen);
        Assert.Equal("kok", chosen!.CatalogKey);
        Assert.Null(chosen.FreeText);
    }

    [Fact]
    public async Task Combobox_lists_results_and_enter_selects_the_active_one()
    {
        CareerDreamChoice? chosen = null;
        var cut = Render<CareerDreamPicker>(p => p
            .Add(x => x.Suggestions, Suggestions(0))
            .Add(x => x.OnChoice, c => chosen = c)
            .Add(x => x.OnSearch, (string _, CancellationToken _) =>
                Task.FromResult<IReadOnlyList<CareerDreamOptionApiItem>>(
                [
                    new CareerDreamOptionApiItem { CatalogKey = "monteur", Title = "Monteur" },
                    new CareerDreamOptionApiItem { CatalogKey = "kok", Title = "Kok" }
                ])));

        await cut.Find("input[role=\"combobox\"]").InputAsync(new() { Value = "mon" });
        cut.WaitForAssertion(
            () => Assert.Contains("career-picker__list is-open", cut.Markup, StringComparison.Ordinal),
            TimeSpan.FromSeconds(3));

        Assert.Equal("true", cut.Find("input[role=\"combobox\"]").GetAttribute("aria-expanded"));
        Assert.Equal(2, cut.FindAll("[role=\"option\"]").Count);

        await cut.Find("input[role=\"combobox\"]").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        cut.WaitForAssertion(() => Assert.NotNull(chosen), TimeSpan.FromSeconds(3));
        Assert.Equal("monteur", chosen!.CatalogKey);
    }

    [Fact]
    public void Free_text_shows_the_validation_error_and_reports_no_choice()
    {
        var reported = 0;
        CareerDreamChoice? chosen = null;
        var cut = Render<CareerDreamPicker>(p => p
            .Add(x => x.Suggestions, Suggestions(0))
            .Add(x => x.OnChoice, c =>
            {
                reported++;
                chosen = c;
            }));

        cut.Find(".career-picker__free-toggle").Click();
        cut.Find(".career-picker__free input").Input("<<<>>>");

        Assert.Contains("career-picker__error", cut.Markup, StringComparison.Ordinal);
        Assert.Null(chosen);

        cut.Find(".career-picker__free input").Input("Kok");
        Assert.NotNull(chosen);
        Assert.Equal("Kok", chosen!.FreeText);
        Assert.True(reported >= 1);
    }

    // ---------- overview ----------

    [Fact]
    public void Overview_shows_the_current_step_with_claws_and_band_facts()
    {
        var plan = Build(PlanJson());
        var cut = Render<CareerOverviewCard>(p => p.Add(x => x.Plan, plan));

        Assert.Equal(1, Occurrences(cut.Markup, "<h1"));
        Assert.Contains("Op weg naar Kok", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Een kreeft groeit alleen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Nog 2 klauwen laten groeien", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("past al past goed bij jou", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("0 jaar", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("%", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Overview_hides_the_course_fact_without_free_courses()
    {
        var plan = Build(PlanJson());
        var cut = Render<CareerOverviewCard>(p => p
            .Add(x => x.Plan, plan)
            .Add(x => x.FreeCourseCount, 0));

        Assert.DoesNotContain("opleidingen ·", cut.Markup, StringComparison.Ordinal);

        cut.Render(p => p.Add(x => x.FreeCourseCount, 1));
        Assert.Contains("opleidingen ·", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Overview_shows_the_local_line_when_the_plan_is_not_from_ai()
    {
        var local = Build(PlanJson(fromAi: false));
        var cut = Render<CareerOverviewCard>(p => p.Add(x => x.Plan, local));
        Assert.Contains("Plan gemaakt op basis van je paspoort.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("met hulp van AI", cut.Markup, StringComparison.Ordinal);

        var ai = Build(PlanJson(fromAi: true));
        var cut2 = Render<CareerOverviewCard>(p => p.Add(x => x.Plan, ai));
        Assert.Contains("met hulp van AI", cut2.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Overview_shows_the_language_line_only_when_the_plan_language_differs()
    {
        var same = CareerPlanViewBuilder.BuildPage(PlanJson(), "nl");
        var cut = Render<CareerOverviewCard>(p => p.Add(x => x.Plan, same));
        Assert.DoesNotContain("career-card__language", cut.Markup, StringComparison.Ordinal);

        var differs = CareerPlanViewBuilder.BuildPage(PlanJson(), "en");
        var cut2 = Render<CareerOverviewCard>(p => p.Add(x => x.Plan, differs));
        Assert.Contains("career-card__language", cut2.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Goal_reached_offers_vacancies_only_with_the_employer_gate_on()
    {
        var plan = Build(PlanJson(allDone: true));

        var off = Render<CareerOverviewCard>(p => p
            .Add(x => x.Plan, plan)
            .Add(x => x.EmployersOn, false));
        Assert.Contains("Je bent er!", off.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Bekijk vacatures", off.Markup, StringComparison.Ordinal);
        Assert.Contains("Bekijk je paspoort", off.Markup, StringComparison.Ordinal);

        var on = Render<CareerOverviewCard>(p => p
            .Add(x => x.Plan, plan)
            .Add(x => x.EmployersOn, true));
        Assert.Contains("Bekijk vacatures", on.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Carried_over_line_is_shown_once_and_can_be_dismissed()
    {
        var plan = Build(PlanJson(carriedOver: 2));
        var dismissed = false;
        var cut = Render<CareerOverviewCard>(p => p
            .Add(x => x.Plan, plan)
            .Add(x => x.ShowCarriedOver, true)
            .Add(x => x.OnDismissCarriedOver, () => dismissed = true));

        Assert.Contains("Je hebt al 2 stappen gehaald", cut.Markup, StringComparison.Ordinal);
        cut.Find(".career-note .career-btn--text").Click();
        Assert.True(dismissed);
    }

    [Fact]
    public void Already_have_section_is_hidden_when_nothing_matched()
    {
        var fresh = Build(PlanJson(nothingCompleted: true));
        var cut = Render<CareerOverviewCard>(p => p.Add(x => x.Plan, fresh));
        Assert.DoesNotContain("career-have", cut.Markup, StringComparison.Ordinal);

        var withProof = Build(PlanJson(onProfileCourse: true));
        var cut2 = Render<CareerOverviewCard>(p => p
            .Add(x => x.Plan, withProof)
            .Add(x => x.PassportOn, true));
        Assert.Contains("career-have", cut2.Markup, StringComparison.Ordinal);
        Assert.Contains("In je paspoort", cut2.Markup, StringComparison.Ordinal);

        var cut3 = Render<CareerOverviewCard>(p => p
            .Add(x => x.Plan, withProof)
            .Add(x => x.PassportOn, false));
        Assert.Contains("In je profiel", cut3.Markup, StringComparison.Ordinal);
    }

    // ---------- stepper + rail ----------

    [Fact]
    public void Stepper_uses_step_short_titles_and_marks_the_current_step()
    {
        var plan = Build(PlanJson());
        var cut = Render<GrowingShellsStepper>(p => p.Add(x => x.Stones, plan.Stones));

        Assert.Contains("Basisdiploma", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Keukenervaring", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"step\"", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(cut.Markup, "aria-current=\"step\""));
        Assert.DoesNotContain("Basis<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Rail_never_invents_a_current_job()
    {
        var plan = Build(PlanJson());
        var cut = Render<CareerRail>(p => p
            .Add(x => x.Plan, plan)
            .Add(x => x.NowLabel, ""));

        Assert.Contains("Waar je nu bent", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("In de diepte", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Naar het licht", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("1 van 3 nieuwe schalen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Alles is bewaard", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_rail_says_the_plan_is_private()
    {
        var cut = Render<CareerRail>(p => p
            .Add(x => x.Plan, CareerPlanViewModel.Empty));

        Assert.Contains("Nog geen droombaan gekozen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je plan is alleen voor jou.", cut.Markup, StringComparison.Ordinal);
    }

    // ---------- dream dialog ----------

    [Fact]
    public void Dialog_has_no_auto_yes_path()
    {
        var confirmed = 0;
        var cancelled = 0;
        var cut = Render<CareerDreamDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.CurrentDream, "Kok")
            .Add(x => x.Suggestions, Suggestions(2))
            .Add(x => x.HasChoice, false)
            .Add(x => x.OnConfirm, () => confirmed++)
            .Add(x => x.OnCancel, () => cancelled++));

        Assert.Contains("Een andere droombaan kiezen?", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Blijf bij Kok", cut.Markup, StringComparison.Ordinal);
        Assert.True(cut.Find(".career-dialog__actions .career-btn--primary").HasAttribute("disabled"));

        cut.Find(".career-dialog__actions .career-btn--secondary").Click();
        Assert.Equal(1, cancelled);
        Assert.Equal(0, confirmed);
    }

    [Fact]
    public void Dialog_promises_nothing_is_lost()
    {
        var cut = Render<CareerDreamDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.CurrentDream, "Kok")
            .Add(x => x.ProofCourseCount, 1)
            .Add(x => x.ProofHasExperience, true));

        Assert.Contains("1 cursus en je werkervaring", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je begint niet opnieuw.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("30 dagen", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Dialog_error_keeps_the_plan_visible()
    {
        var confirmed = 0;
        var cut = Render<CareerDreamDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.CurrentDream, "Kok")
            .Add(x => x.ErrorText, "Je kunt morgen weer een nieuw plan maken.")
            .Add(x => x.OnConfirm, () => confirmed++));

        Assert.Contains("role=\"alert\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je kunt morgen weer een nieuw plan maken.", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, confirmed);
    }

    [Fact]
    public void Archived_plans_can_be_restored_from_the_ui()
    {
        Guid? restored = null;
        var planId = Guid.NewGuid();
        var cut = Render<CareerArchivedPlans>(p => p
            .Add(x => x.Plans, new List<ArchivedCareerPlanApiModel>
            {
                new()
                {
                    PlanId = planId,
                    DreamTitle = "Monteur",
                    ArchivedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                    CompletedSteps = 1,
                    TotalSteps = 3
                }
            })
            .Add(x => x.OnRestore, id => restored = id));

        Assert.Contains("Eerdere plannen (1)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Monteur", cut.Markup, StringComparison.Ordinal);

        cut.Find(".career-archive__list .career-btn--text").Click();
        Assert.Contains("Terug naar Monteur?", cut.Markup, StringComparison.Ordinal);

        cut.Find(".career-dialog__actions .career-btn--primary").Click();
        Assert.Equal(planId, restored);
    }

    [Fact]
    public void Archive_disclosure_is_hidden_without_archived_plans()
    {
        var cut = Render<CareerArchivedPlans>(p => p
            .Add(x => x.Plans, new List<ArchivedCareerPlanApiModel>()));

        Assert.DoesNotContain("career-archive", cut.Markup, StringComparison.Ordinal);
    }

    // ---------- scene + RTL ----------

    [Fact]
    public void Scene_is_hidden_from_assistive_tech_and_carries_no_hard_coded_colour()
    {
        var plan = Build(PlanJson());
        var stones = plan.Stones
            .Select(s => new ClimbStone(s.Label, s.State, s.Number?.ToString()))
            .ToList();

        var cut = Render<CareerClimbScene>(p => p
            .Add(x => x.Stones, stones)
            .Add(x => x.CurrentIndex, plan.CurrentStoneIndex)
            .Add(x => x.LobsterSize, plan.LobsterSizeDesktop)
            .Add(x => x.PlatesShed, plan.PlatesShed));

        Assert.Contains("aria-hidden=\"true\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("career-scene__stone--dream", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("career-scene__shard", cut.Markup, StringComparison.Ordinal);
        // No inline colours in the career layer itself; the shared JourneyLobster mask is exempt.
        Assert.DoesNotContain("career-scene__stone\" fill", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Arabic_renders_right_to_left_and_mirrors_the_scene()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("ar");
        Assert.True(culture.IsRightToLeft);

        var cut = Render<CareerEmptyCard>(p => p
            .Add(x => x.Suggestions, Suggestions(2)));
        Assert.Contains("إلى أين تريد أن تنمو؟", cut.Markup, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(
            RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "carriere.css"));
        Assert.Contains("[dir=\"rtl\"] .career-scene__svg", css, StringComparison.Ordinal);
        Assert.Contains("scaleX(-1)", css, StringComparison.Ordinal);
        Assert.Contains("inset-inline-start", css, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(
            RepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("rtl", layout, StringComparison.Ordinal);

        await culture.SetLanguageAsync("nl");
    }

    // ---------- helpers ----------

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        var i = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (i >= 0)
        {
            count++;
            i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static CareerPlanViewModel Build(CareerPathPlanApiModel plan)
        => CareerPlanViewBuilder.BuildPage(plan, "nl");

    private static List<CareerDreamOptionApiItem> Suggestions(int count)
    {
        var all = new List<CareerDreamOptionApiItem>
        {
            new() { CatalogKey = "kok", Title = "Kok", ReasonKey = "CareerDream.Reason.Test" },
            new() { CatalogKey = "monteur", Title = "Monteur", ReasonKey = "CareerDream.Reason.Wish" },
            new() { CatalogKey = "leraar", Title = "Leraar", ReasonKey = "CareerDream.Reason.Wish" }
        };

        return all.Take(count).ToList();
    }

    private static CareerPathPlanApiModel PlanJson(
        bool fromAi = false,
        bool allDone = false,
        int carriedOver = 0,
        bool onProfileCourse = false,
        bool nothingCompleted = false)
        => new()
        {
            DreamTitle = "Kok",
            FromAi = fromAi,
            PlanLanguage = "nl",
            DreamFitBand = "Good",
            CarriedOverCount = carriedOver,
            GoalReached = allDone,
            Steps =
            [
                new CareerPathStepApiModel
                {
                    Id = "s1",
                    Order = 1,
                    Title = "Basisdiploma keuken",
                    Status = nothingCompleted ? "Active" : "Completed",
                    StepFitBand = "Good",
                    CourseStatuses = onProfileCourse
                        ? [new CareerPathCourseApiModel { Name = "Basiscursus koken", OnProfile = true }]
                        : []
                },
                new CareerPathStepApiModel
                {
                    Id = "s2",
                    Order = 2,
                    Title = "Keukenervaring opdoen",
                    Status = allDone ? "Completed" : nothingCompleted ? "Open" : "Active",
                    StepFitBand = "Good",
                    SkillsGap = ["Snijtechniek", "Werken onder druk"],
                    CourseStatuses = [new CareerPathCourseApiModel { Name = "Veilig werken", OnProfile = false }]
                },
                new CareerPathStepApiModel
                {
                    Id = "s3",
                    Order = 3,
                    Title = "Zelfstandig koken",
                    Status = allDone ? "Completed" : "Open",
                    StepFitBand = ""
                }
            ]
        };

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "t"), new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }
}
