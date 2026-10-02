using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Components.Candidate.Career;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Jobsy.Web.Services.Careers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// Carrière 03: the step detail (claws, band, vacancies, courses, proof), the growth moment
/// and undo. No percentages, no "0 jaar", no clickable AI course names.
/// </summary>
public class CareerStepBunitTests : TestContext
{
    /// <summary>D4: no percentage is ever rendered on a career surface.</summary>
    private static readonly System.Text.RegularExpressions.Regex Percentage = new(@"\d\s*%");

    public CareerStepBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var http = new HttpClient(new FakeHandler()) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new JobsyApiClient(http));
        // CourseSuggestionBlock describes load failures via UserFacingError (errors 04).
        Services.AddSingleton(sp => new Jobsy.Web.Services.UserFacingError(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Jobsy.Web.Services.UserFacingError>.Instance,
            sp.GetRequiredService<CultureState>()));
    }

    // ---------- detail: active / done / todo ----------

    [Fact]
    public void Active_step_shows_the_growing_eyebrow_and_one_primary_action()
    {
        var cut = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, Step(2)));

        Assert.Equal(1, Occurrences(cut.Markup, "<h1"));
        Assert.Contains("De klim · stap 2 van 3 · groeit nu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Keukenervaring opdoen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Mijn groeireis", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Deze stap is klaar", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Toch nog niet klaar", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotMatch(Percentage, cut.Markup);
    }

    [Fact]
    public void Done_step_offers_undo_only_when_it_is_the_last_completed_step()
    {
        var last = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, Step(1, allDone: false)));
        Assert.Contains("· gehaald", last.Markup, StringComparison.Ordinal);
        Assert.Contains("Toch nog niet klaar", last.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Deze stap is klaar", last.Markup, StringComparison.Ordinal);

        // Step 1 of a fully completed plan is no longer the last completed step (D9).
        var earlier = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, Step(1, allDone: true)));
        Assert.DoesNotContain("Toch nog niet klaar", earlier.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Todo_step_is_read_only_and_says_which_step_comes_first()
    {
        var cut = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, Step(3)));

        Assert.Contains("· later", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Eerst stap 2 afmaken.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Deze stap is klaar", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Toch nog niet klaar", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Completing_reports_once_and_stays_quiet_while_busy()
    {
        var completed = 0;
        var cut = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.OnComplete, () => completed++));

        cut.Find(".career-only-desktop .career-btn--primary").Click();
        Assert.Equal(1, completed);

        cut.SetParametersAndRender(p => p.Add(x => x.Busy, true));
        Assert.True(cut.Find(".career-only-desktop .career-btn--primary").HasAttribute("disabled"));
    }

    // ---------- gaps and years ----------

    [Fact]
    public void Gaps_show_missing_claws_and_present_items_with_show_all()
    {
        var step = Step(2, extraGaps: 6);
        var cut = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, step));

        Assert.Contains("Welke klauwen je al hebt", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(6, cut.FindAll(".career-step__gap").Count);
        Assert.Contains("Toon alles (10)", cut.Markup, StringComparison.Ordinal);

        cut.Find(".career-step__block--gaps .career-btn--text").Click();
        Assert.Equal(10, cut.FindAll(".career-step__gap").Count);
        Assert.Contains("heb je al", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Years_line_is_hidden_for_zero_years()
    {
        var zero = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, Step(2)));
        Assert.DoesNotContain("jaar ervaring helpt", zero.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("0 jaar", zero.Markup, StringComparison.Ordinal);

        var two = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, Step(2, years: 2)));
        Assert.Contains("2 jaar ervaring helpt", two.Markup, StringComparison.Ordinal);
    }

    // ---------- band ----------

    [Theory]
    [InlineData("Good", "Past goed", "Deze steen past goed bij jouw formaat.")]
    [InlineData("Fair", "Past redelijk", "Met Snijtechniek worden je matches sterker.")]
    [InlineData("NotYet", "Past nog niet", "Deze steen is nog wat groot.")]
    public void Band_pill_and_sentence_follow_the_band(string band, string pill, string sentence)
    {
        var cut = RenderComponent<CareerStepDetailCard>(p => p.Add(x => x.Step, Step(2, band: band)));

        Assert.Contains("career-pill", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(pill, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(sentence, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotMatch(Percentage, cut.Markup);
    }

    [Fact]
    public void Unknown_band_shows_no_pill_but_the_test_link()
    {
        var cut = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2, band: ""))
            .Add(x => x.PassportOn, true));

        Assert.DoesNotContain("career-pill", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Doe de Beroepen-test in je paspoort", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("tab=tests", cut.Markup, StringComparison.Ordinal);
    }

    // ---------- vacancies (D + E) ----------

    [Fact]
    public void Vacancy_link_is_hidden_with_the_employer_gate_off()
    {
        var off = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.EmployersOn, false)
            .Add(x => x.VacancyFit, new CareerStepVacancyFit(GateOpen: true, GoodCount: 14)));

        Assert.DoesNotContain("Vacatures voor", off.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("vacatures passen goed", off.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Vacancy_count_needs_an_open_fit_gate_and_at_least_one_match()
    {
        var closed = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.VacancyFit, new CareerStepVacancyFit(GateOpen: false, GoodCount: 14)));
        Assert.Contains("Vacatures voor Keukenervaring", closed.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("vacatures passen goed", closed.Markup, StringComparison.Ordinal);

        var none = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.VacancyFit, new CareerStepVacancyFit(GateOpen: true, GoodCount: 0)));
        Assert.DoesNotContain("vacatures passen goed", none.Markup, StringComparison.Ordinal);

        var open = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.VacancyFit, new CareerStepVacancyFit(GateOpen: true, GoodCount: 14)));
        Assert.Contains("14 vacatures passen goed", open.Markup, StringComparison.Ordinal);
    }

    // ---------- courses (Dependency B) ----------

    [Fact]
    public void Course_block_puts_free_first_labels_the_partner_and_discloses_it()
    {
        var cut = RenderComponent<CareerStepCourses>(p => p
            .Add(x => x.Slots, Slots())
            .Add(x => x.CourseNames, new List<string> { "Veilig werken" })
            .Add(x => x.SkillLabel, "Snijtechniek"));

        var titles = cut.FindAll(".passport-course__title").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(["Gratis basiscursus", "Partnercursus"], titles);
        Assert.Contains("Gratis staat altijd bovenaan.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Partnerlink: Lobsy kan een vergoeding krijgen.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("rel=\"sponsored noopener noreferrer\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Snijtechniek", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll(".passport-course__item--partner"));
    }

    [Fact]
    public void Without_a_curated_match_the_block_is_hidden_and_course_names_are_plain_text()
    {
        var cut = RenderComponent<CareerStepCourses>(p => p
            .Add(x => x.Slots, new List<PassportCourseCard>())
            .Add(x => x.CourseNames, new List<string> { "Veilig werken", "Snijcursus" }));

        Assert.DoesNotContain("passport-course__item", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Gratis staat altijd bovenaan.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Wat kan helpen: Veilig werken, Snijcursus", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<a", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_renders_without_slots_and_without_course_names()
    {
        var cut = RenderComponent<CareerStepCourses>(p => p
            .Add(x => x.Slots, new List<PassportCourseCard>())
            .Add(x => x.CourseNames, new List<string>()));

        Assert.Equal("", cut.Markup.Trim());
    }

    // ---------- proof (Dependency F) ----------

    [Fact]
    public void Proof_link_opens_the_passport_bewijzen_tab_with_the_name_prefilled()
    {
        var cut = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.PassportOn, true));

        var href = cut.Find(".career-only-desktop a.career-btn--text").GetAttribute("href");
        Assert.Equal("/candidate/paspoort?tab=proof&add=certificate&name=Veilig%20werken", href);

        var label = cut.Find(".career-only-mobile a.career-btn--icon").GetAttribute("aria-label");
        Assert.Equal("Bewijs toevoegen", label);
    }

    [Fact]
    public void Without_the_passport_flag_proof_opens_the_profile_certificates()
    {
        var cut = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.PassportOn, false));

        var href = cut.Find(".career-only-desktop a.career-btn--text").GetAttribute("href");
        Assert.Contains("/candidate/profile?add=certificate", href, StringComparison.Ordinal);
        Assert.Contains("#certificates", href, StringComparison.Ordinal);
    }

    // ---------- growth moment ----------

    [Fact]
    public void Done_card_replaces_the_toast_and_focuses_its_heading()
    {
        var cut = RenderComponent<CareerStepDoneCard>(p => p
            .Add(x => x.StepNumber, 2)
            .Add(x => x.TotalSteps, 3)
            .Add(x => x.StepTitle, "Keukenervaring opdoen")
            .Add(x => x.ShortTitle, "Keukenervaring")
            .Add(x => x.Stones, Plan().Stones)
            .Add(x => x.NextTitle, "Zelfstandig koken")
            .Add(x => x.NextNumber, 3)
            .Add(x => x.NextGapCount, 2)
            .Add(x => x.NextCourseCount, 1));

        Assert.Contains("De klim · stap 2 van 3 klaar", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je nieuwe schaal past", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Keukenervaring opdoen is gehaald.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Oude schaal", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Stap 3: Zelfstandig koken", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Nog 2 klauwen · 1 opleidingen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Op naar stap 3", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"-1\"", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("LobsyToast", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotMatch(Percentage, cut.Markup);
    }

    [Fact]
    public void Gained_list_claims_only_true_items()
    {
        var bare = RenderComponent<CareerStepDoneCard>(p => p
            .Add(x => x.StepNumber, 2)
            .Add(x => x.TotalSteps, 3)
            .Add(x => x.StepTitle, "Keukenervaring opdoen"));

        Assert.Contains("Stap 2 staat als gehaald in je plan.", bare.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("staat in je paspoort", bare.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("vacatures", bare.Markup, StringComparison.Ordinal);

        var full = RenderComponent<CareerStepDoneCard>(p => p
            .Add(x => x.StepNumber, 2)
            .Add(x => x.TotalSteps, 3)
            .Add(x => x.StepTitle, "Keukenervaring opdoen")
            .Add(x => x.ShortTitle, "helpende")
            .Add(x => x.PassportOn, true)
            .Add(x => x.ProofName, "Veilig werken")
            .Add(x => x.GrownClaws, new List<string> { "Snijtechniek" })
            .Add(x => x.VacancyCount, 14));

        Assert.Contains("Veilig werken staat in je paspoort", full.Markup, StringComparison.Ordinal);
        Assert.Contains("Je klauw ‘Snijtechniek’ is gegroeid", full.Markup, StringComparison.Ordinal);
        Assert.Contains("Nieuw: 14 vacatures als helpende passen bij je", full.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("staat als gehaald in je plan", full.Markup, StringComparison.Ordinal);
        Assert.Equal(3, full.FindAll(".career-done__gained li").Count);
    }

    [Fact]
    public void Vacancy_gain_line_is_absent_when_the_gate_left_the_count_at_zero()
    {
        var cut = RenderComponent<CareerStepDoneCard>(p => p
            .Add(x => x.StepNumber, 2)
            .Add(x => x.TotalSteps, 3)
            .Add(x => x.StepTitle, "Keukenervaring opdoen")
            .Add(x => x.ProofName, "Veilig werken")
            .Add(x => x.VacancyCount, 0));

        Assert.DoesNotContain("vacatures", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Last_step_points_at_the_dream_job_instead_of_a_next_step()
    {
        var cut = RenderComponent<CareerStepDoneCard>(p => p
            .Add(x => x.StepNumber, 3)
            .Add(x => x.TotalSteps, 3)
            .Add(x => x.StepTitle, "Zelfstandig koken"));

        Assert.Contains("Bekijk je droombaan", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Op naar stap", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Je volgende steen", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Undo_is_always_within_reach_on_the_done_card()
    {
        var undone = 0;
        var cut = RenderComponent<CareerStepDoneCard>(p => p
            .Add(x => x.StepNumber, 2)
            .Add(x => x.TotalSteps, 3)
            .Add(x => x.StepTitle, "Keukenervaring opdoen")
            .Add(x => x.OnUndo, () => undone++));

        cut.Find(".career-btn--text").Click();
        Assert.Equal(1, undone);
    }

    [Fact]
    public void Undo_conflict_code_is_shown_as_friendly_text()
    {
        var cut = RenderComponent<CareerStepDoneCard>(p => p
            .Add(x => x.StepNumber, 2)
            .Add(x => x.TotalSteps, 3)
            .Add(x => x.StepTitle, "Keukenervaring opdoen")
            .Add(x => x.ErrorText, ErrorTextFor("undo_last_first")));

        Assert.Contains("role=\"alert\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je kunt alleen je laatste stap terugzetten.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Completing_a_non_active_step_shows_the_conflict_text_from_the_code()
    {
        var error = ErrorTextFor("complete_previous_first");
        Assert.Equal("Maak eerst de stap ervoor af.", error);

        var cut = RenderComponent<CareerStepDetailCard>(p => p
            .Add(x => x.Step, Step(2))
            .Add(x => x.ErrorText, error));

        Assert.Contains("role=\"alert\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Maak eerst de stap ervoor af.", cut.Markup, StringComparison.Ordinal);
    }

    // ---------- announcements and motion contracts ----------

    [Fact]
    public void Completion_and_undo_have_polite_announcements()
    {
        var culture = Services.GetRequiredService<CultureState>();
        Assert.Equal(
            "Stap 2 is klaar. Je nieuwe schaal past.",
            culture.Format("Career.Done.Announce", 2));
        Assert.Equal("Stap 2 staat weer open.", culture.Format("Career.Undo.Announce", 2));

        var page = File.ReadAllText(Path.Combine(
            RepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "CareerDashboard.razor"));
        Assert.Contains("aria-live=\"polite\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("LobsyToast", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduced_motion_disables_the_falling_shell_plate()
    {
        var css = File.ReadAllText(Path.Combine(
            RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "carriere.css"));

        var reduced = css[css.IndexOf("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal)..];
        Assert.Contains(".career-scene--celebrate .journey-plate--falling", reduced, StringComparison.Ordinal);
        Assert.Contains(".career-scene--celebrate .journey-lob__fall", reduced, StringComparison.Ordinal);
        Assert.Contains("animation: none", reduced, StringComparison.Ordinal);
    }

    // ---------- routing and builder ----------

    [Fact]
    public void Overview_links_to_the_step_as_a_query_on_carriere()
    {
        var cut = RenderComponent<CareerOverviewCard>(p => p.Add(x => x.Plan, Plan()));

        Assert.Contains("href=\"/carriere?stap=2\"", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("/carriere/stap/", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Step_detail_is_a_query_on_the_carriere_page_not_a_route_of_its_own()
    {
        var page = File.ReadAllText(Path.Combine(
            RepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "CareerDashboard.razor"));

        Assert.Contains("SupplyParameterFromQuery(Name = \"stap\")", page, StringComparison.Ordinal);
        Assert.DoesNotContain("/carriere/stap", page, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(page, "@page \""));
    }

    [Fact]
    public void Unknown_step_order_falls_back_to_the_overview()
    {
        Assert.Null(CareerPlanViewBuilder.BuildStep(PlanJson(), 9));
        Assert.Null(CareerPlanViewBuilder.BuildStep(PlanJson(), 0));
        Assert.Null(CareerPlanViewBuilder.BuildStep(null, 1));
    }

    [Fact]
    public void Step_title_drops_the_level_and_the_lead_keeps_two_sentences()
    {
        var plan = PlanJson();
        plan.Steps[1].Title = "Keukenervaring opdoen (MBO 2)";
        plan.Steps[1].Summary = "Eerste zin. Tweede zin. Derde zin die wegvalt.";

        var step = CareerPlanViewBuilder.BuildStep(plan, 2);

        Assert.NotNull(step);
        Assert.Equal("Keukenervaring opdoen", step!.Title);
        Assert.Equal("MBO 2", step.Level);
        Assert.Equal("Eerste zin. Tweede zin.", step.Lead);
    }

    [Fact]
    public void Build_done_claims_only_proof_and_claws_that_really_landed()
    {
        var before = CareerPlanViewBuilder.BuildStep(PlanJson(), 2);
        Assert.NotNull(before);
        Assert.Contains(before!.Missing, m => m.Text == "Snijtechniek");

        var after = PlanJson();
        var step = after.Steps.First(s => s.Order == 2);
        step.Status = "Completed";
        step.CourseStatuses = [new CareerPathCourseApiModel { Name = "Veilig werken", OnProfile = true }];
        step.SkillsGap = ["Werken onder druk"];

        var done = CareerPlanViewBuilder.BuildDone(before, after, 2);

        Assert.Equal("Veilig werken", done.ProofName);
        Assert.Empty(done.GrownClaws);
    }

    // ---------- helpers ----------

    /// <summary>The UI never shows <c>ex.Message</c>: codes map to <c>CareerErr.*</c> (B12).</summary>
    private string ErrorTextFor(string code)
    {
        var culture = Services.GetRequiredService<CultureState>();
        return CareerPathService.ErrorMessage(
            key => culture[key],
            new CareerApiError(code, System.Net.HttpStatusCode.Conflict));
    }

    private static CareerPlanViewModel Plan()
        => CareerPlanViewBuilder.BuildPage(PlanJson(), "nl");

    private static CareerStepDetailView Step(
        int order,
        bool allDone = false,
        string band = "Good",
        int years = 0,
        int extraGaps = 0)
    {
        var plan = PlanJson(allDone);
        var step = plan.Steps.First(s => s.Order == order);
        step.StepFitBand = band;
        step.YearsExperienceNeeded = years;
        if (extraGaps > 0)
        {
            var gaps = step.SkillsGap.ToList();
            for (var i = 0; i < extraGaps; i++)
            {
                gaps.Add($"Klauw {i + 1}");
            }

            step.SkillsGap = gaps;
        }

        var built = CareerPlanViewBuilder.BuildStep(plan, order);
        Assert.NotNull(built);
        return built!;
    }

    private static List<PassportCourseCard> Slots() =>
    [
        new()
        {
            OfferId = Guid.NewGuid(),
            Title = "Gratis basiscursus",
            ProviderName = "Leerwerkloket",
            Type = "Cursus",
            Delivery = "Online",
            IsFree = true,
            Rel = TrainingTracking.RelFor(false)
        },
        new()
        {
            OfferId = Guid.NewGuid(),
            Title = "Partnercursus",
            ProviderName = "Partner BV",
            Type = "Cursus",
            Delivery = "Online",
            IsPartner = true,
            Rel = TrainingTracking.RelFor(true)
        }
    ];

    private static CareerPathPlanApiModel PlanJson(bool allDone = false) => new()
    {
        DreamTitle = "Kok",
        PlanLanguage = "nl",
        DreamFitBand = "Good",
        GoalReached = allDone,
        Steps =
        [
            new CareerPathStepApiModel
            {
                Id = "s1",
                Order = 1,
                Title = "Basisdiploma keuken",
                Status = "Completed",
                StepFitBand = "Good",
                Summary = "Je haalt je basisdiploma."
            },
            new CareerPathStepApiModel
            {
                Id = "s2",
                Order = 2,
                Title = "Keukenervaring opdoen",
                Status = allDone ? "Completed" : "Active",
                StepFitBand = "Good",
                Summary = "Je werkt in een keuken en leert het vak.",
                SkillsGap = ["Snijtechniek", "Werken onder druk"],
                MinRequirements = ["Diploma sociale hygiëne"],
                CourseStatuses =
                [
                    new CareerPathCourseApiModel { Name = "Veilig werken", OnProfile = false },
                    new CareerPathCourseApiModel { Name = "Basiscursus koken", OnProfile = true }
                ]
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

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "t"), new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }
}
