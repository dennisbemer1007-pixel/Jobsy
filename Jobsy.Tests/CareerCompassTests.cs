using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class CareerCompassTests
{
    [Fact]
    public void Bands_use_inclusive_thresholds_95_85_75()
    {
        Assert.Equal("super", CareerCompassBuilder.Band(100));
        Assert.Equal("super", CareerCompassBuilder.Band(95));
        Assert.Equal("strong", CareerCompassBuilder.Band(94));
        Assert.Equal("strong", CareerCompassBuilder.Band(85));
        Assert.Equal("broaden", CareerCompassBuilder.Band(84));
        Assert.Equal("broaden", CareerCompassBuilder.Band(75));
        Assert.Equal("", CareerCompassBuilder.Band(74));
    }

    [Fact]
    public void High_hands_on_scores_fill_super_match_occupations()
    {
        var compass = CareerCompassBuilder.Build(HandsOnScores(), fromDeepAnalysis: true);
        Assert.True(compass.FromDeepAnalysis);
        Assert.Contains("Aanpakken met je handen", compass.Strengths);
        Assert.Contains(compass.SuperMatches, m => m.Title.Contains("kas", StringComparison.OrdinalIgnoreCase) && m.Percent >= 95);
        Assert.Contains(compass.SuperMatches, m => m.Title.Contains("bouw", StringComparison.OrdinalIgnoreCase));
        Assert.All(compass.SuperMatches, m => Assert.True(m.Percent >= 95));
        Assert.All(compass.StrongChoices, m => Assert.InRange(m.Percent, 85, 94));
        Assert.All(compass.Broadening, m => Assert.InRange(m.Percent, 75, 84));
        Assert.Contains(compass.PracticalNotes, n => n.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(compass.PracticalNotes, n => n == TrainingCopy.GapAdvice);
        Assert.DoesNotContain(compass.PracticalNotes, n => n.Contains("150 vragen", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Mixed_scores_span_all_three_occupation_bands()
    {
        var compass = CareerCompassBuilder.Build(new RiasecScores(100, 40, 40, 88, 40, 78));
        Assert.True(compass.HasOccupations);
        Assert.NotEmpty(compass.SuperMatches);
        Assert.NotEmpty(compass.StrongChoices);
        Assert.NotEmpty(compass.Broadening);
        Assert.Contains(compass.SuperMatches, m => m.Title.Contains("kas", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(compass.StrongChoices, m => m.Title.Contains("zorg", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(compass.Broadening, m => m.Title.Contains("Administratief", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Quick_scan_practical_notes_invite_the_paid_test()
    {
        var compass = CareerCompassBuilder.Build(HandsOnScores(), fromDeepAnalysis: false);
        Assert.False(compass.FromDeepAnalysis);
        Assert.Contains(
            compass.PracticalNotes,
            n => n.Contains("uitgebreide beroepentest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Incomplete_scores_yield_empty_occupations_and_a_plain_prompt()
    {
        var compass = CareerCompassBuilder.Build(null);
        Assert.False(compass.HasOccupations);
        Assert.NotEmpty(compass.PracticalNotes);
        AssertNoJargon(compass);
    }

    [Fact]
    public void Candidate_facing_copy_has_no_riasec_or_ocean_jargon()
    {
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon("sociale werkplek"));
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon("RIASEC-profiel"));
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon("Holland-code ACE"));
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon("extraversie"));
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon("neuroticisme"));

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            AssertNoJargon(CareerCompassBuilder.TypeLabel(code));
        }

        AssertNoJargon(CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandSuper));
        AssertNoJargon(CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandStrong));
        AssertNoJargon(CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandBroaden));

        foreach (var fromDeep in new[] { false, true })
        {
            foreach (var code in CareerTestCatalog.RiasecCodes)
            {
                AssertNoJargon(CareerCompassBuilder.Build(Peak(code), fromDeep));
            }
        }

        var advice = DeepAnalysisCatalog.CareerAdviceParagraphs(
            CareerTestCatalog.RiasecCodes.Select(c => new DeepAnalysisDomainScore(c, 90, 25)).ToList());
        foreach (var line in advice)
        {
            AssertNoJargon(line);
        }
    }

    [Fact]
    public void Kompas_ui_strings_use_plain_dutch()
    {
        string[] keys =
        [
            "Kompas.Career",
            "Kompas.CareerStrengths",
            "Kompas.BandSuper",
            "Kompas.BandStrong",
            "Kompas.BandBroaden",
            "Kompas.PracticalTitle",
            "Career.Lead",
            "Career.ScienceNote",
            "Career.SavedComplete"
        ];
        foreach (var key in keys)
        {
            var nl = UiStrings.Get(key, "nl");
            Assert.False(string.IsNullOrWhiteSpace(nl));
            AssertNoJargon(nl);
        }

        Assert.Equal("Mijn Beroepen-kompas", UiStrings.Get("Kompas.Career", "nl"));
        Assert.Equal("Wat betekent dit voor jou?", UiStrings.Get("Kompas.PracticalTitle", "nl"));
        Assert.Contains("95%", UiStrings.Get("Kompas.BandSuper", "nl"));
        Assert.Contains("kernfit", UiStrings.Get("Kompas.BandSuper", "nl"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("85%", UiStrings.Get("Kompas.BandStrong", "nl"));
        Assert.Contains("75%", UiStrings.Get("Kompas.BandBroaden", "nl"));
        Assert.Equal("Mijn profiel", UiStrings.Get("Kompas.TabProfile", "nl"));
        Assert.Equal("Mijn competenties", UiStrings.Get("Kompas.TabCompetencies", "nl"));
        Assert.Equal("Mijn beste match", UiStrings.Get("Kompas.TabCareers", "nl"));
    }

    [Fact]
    public void Brand_logo_png_is_colored()
    {
        var png = LoadBrandLogo();
        Assert.True(png.Length > 100);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal((byte)'N', png[2]);
        Assert.Equal((byte)'G', png[3]);
        // IHDR color type: 2 truecolor, 3 indexed, 6 truecolor+alpha — not grayscale.
        var colorType = png[25];
        Assert.True(colorType is 2 or 3 or 6, $"Expected a color PNG, got color type {colorType}.");
    }

    [Fact]
    public void Career_pdf_uses_logo_bands_and_plain_language()
    {
        var compass = CareerCompassBuilder.Build(HandsOnScores(), fromDeepAnalysis: true);
        Assert.True(compass.HasOccupations);
        Assert.Contains(compass.PracticalNotes, n => n.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
        AssertNoJargon(compass);

        var withLogo = AssessmentReportPdfService.RenderCareer(
            "Lobsy", LoadBrandLogo(), "Ada Kandidaat", "20 september 2026", compass);
        var withoutLogo = AssessmentReportPdfService.RenderCareer(
            "Lobsy", [], "Ada Kandidaat", "20 september 2026", compass);

        AssertPdf(withLogo);
        AssertPdf(withoutLogo);
        Assert.True(withLogo.Length > withoutLogo.Length);

        var source = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Infrastructure/Services/AssessmentReportPdfService.cs"));
        var renderStart = source.IndexOf("internal static byte[] RenderCareer(", StringComparison.Ordinal);
        Assert.True(renderStart >= 0);
        var renderCompetence = source.IndexOf("private static byte[] RenderCompetence(", renderStart, StringComparison.Ordinal);
        var renderCareer = source[renderStart..(renderCompetence > renderStart ? renderCompetence : source.Length)];
        Assert.Contains("GetBrandLogoPng", source, StringComparison.Ordinal);
        Assert.Contains("Jouw loopbaanrapport", renderCareer, StringComparison.Ordinal);
        Assert.Contains("Wat betekent dit voor jou?", renderCareer, StringComparison.Ordinal);
        Assert.Contains("persoonlijk en positief", renderCareer, StringComparison.Ordinal);
        Assert.Contains("WriteOccupationBand", renderCareer, StringComparison.Ordinal);
        Assert.Contains("BandSuper", renderCareer, StringComparison.Ordinal);
        Assert.Contains("BandStrong", renderCareer, StringComparison.Ordinal);
        Assert.Contains("BandBroaden", renderCareer, StringComparison.Ordinal);
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(renderCareer));
    }

    [Fact]
    public void Score_based_vacancy_fit_uses_candidate_percentages()
    {
        var scores = new RiasecScores(100, 0, 0, 10, 0, 0);
        Assert.Equal(1.0, VacancyRiasecProfile.Fit01(scores, [CareerTestCatalog.Realistic]));
        Assert.Equal(0.10, VacancyRiasecProfile.Fit01(scores, [CareerTestCatalog.Social]), 2);
    }

    [Fact]
    public void Deep_analysis_domain_scores_map_onto_riasec_percents()
    {
        var mapped = DeepAnalysisCatalog.ToRiasecScores(
        [
            new DeepAnalysisDomainScore(CareerTestCatalog.Realistic, 91, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Investigative, 40, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Artistic, 33, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Social, 22, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Enterprising, 11, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Conventional, 5, 25)
        ]);
        Assert.True(mapped.IsComplete);
        Assert.Equal(91, mapped.Realistic);
        Assert.Equal(5, mapped.Conventional);
    }

    [Fact]
    public void Kompas_tabs_normalize_query_and_legacy_hashes()
    {
        Assert.Equal(CandidateKompasTabs.WhoAmI, CandidateKompasTabs.Normalize("wie-ben-ik"));
        Assert.Equal(CandidateKompasTabs.Profile, CandidateKompasTabs.Normalize("profiel"));
        Assert.Equal(CandidateKompasTabs.Competencies, CandidateKompasTabs.Normalize("competency-profile-title"));
        Assert.Equal(CandidateKompasTabs.Career, CandidateKompasTabs.Normalize("#career-profile-title"));
        Assert.Equal(CandidateKompasTabs.Career, CandidateKompasTabs.Normalize("beste-match"));
        Assert.Equal(CandidateKompasTabs.Fit, CandidateKompasTabs.Normalize("functiefit"));
        Assert.Equal(CandidateKompasTabs.Disc, CandidateKompasTabs.Neighbor(CandidateKompasTabs.Competencies, 1));
        Assert.Equal(CandidateKompasTabs.WhoAmI, CandidateKompasTabs.Neighbor(CandidateKompasTabs.Profile, -1));
        Assert.Equal(CandidateKompasTabs.Disc, CandidateKompasTabs.Normalize("gedragsanalyse"));
    }

    [Fact]
    public void Kompas_panel_and_pages_wire_career_compass()
    {
        var root = RepoRoot.Find();
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerCompassPanel.razor"));
        Assert.Contains("Kompas.BandSuper", panel, StringComparison.Ordinal);
        Assert.Contains("Kompas.PracticalTitle", panel, StringComparison.Ordinal);
        Assert.Contains("TrainingOffersBlock", panel, StringComparison.Ordinal);
        Assert.Contains("kompas-occupation__details", panel, StringComparison.Ordinal);
        Assert.Contains("CareerOccupationDetail", panel, StringComparison.Ordinal);

        var home = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.DoesNotContain("Talent.CandidateTitle", home, StringComparison.Ordinal);
        var profilePage = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("ProfileTab", profilePage, StringComparison.Ordinal);
        Assert.Contains("profile-section-nav", profilePage, StringComparison.Ordinal);
        Assert.Contains("Talent.CandidateTitle", profilePage, StringComparison.Ordinal);
        Assert.Contains("CareerCompassPanel", home, StringComparison.Ordinal);
        Assert.Contains("role=\"tablist\"", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.TabWhoAmI", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.TabProfile", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.TabCompetencies", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.TabCulture", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.TabCareers", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.TabFit", home, StringComparison.Ordinal);
        Assert.Contains("AxisCount = 5", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CompetencyScorePanel.razor")), StringComparison.Ordinal);
        var competencyPanel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CompetencyScorePanel.razor"));
        Assert.Contains("competency-skill__details", competencyPanel, StringComparison.Ordinal);
        Assert.Contains("TrainingOffersBlock", competencyPanel, StringComparison.Ordinal);
        Assert.Contains("CampaignCompetence", competencyPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("OCEAN", competencyPanel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RIASEC", competencyPanel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kompas-panel-profile", home, StringComparison.Ordinal);
        Assert.Contains("kompas-panel-competencies", home, StringComparison.Ordinal);
        Assert.Contains("kompas-panel-career", home, StringComparison.Ordinal);
        Assert.Contains("kompas-panel-fit", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.ShowWorkStyle", home, StringComparison.Ordinal);
        Assert.Contains("Kompas.PracticalTitle", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerCompassPanel.razor")), StringComparison.Ordinal);
        Assert.DoesNotContain("kompas-grid", home, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".kompas-tabs.admin-sublinks {\n    position: sticky;\n    top: 0;\n    z-index: 6;\n    display: flex;\n    flex-direction: row;\n    flex-wrap: wrap;", css, StringComparison.Ordinal);
        Assert.Contains(".kompas-workspace--with-side", css, StringComparison.Ordinal);
        Assert.Contains(".competency-match-card__head", css, StringComparison.Ordinal);
        Assert.Contains(".kompas-status-stack", css, StringComparison.Ordinal);
        Assert.Contains(".kompas-card[hidden]", css, StringComparison.Ordinal);
        var minCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.min.css"));
        Assert.Contains(".kompas-tabs.admin-sublinks", minCss, StringComparison.Ordinal);
        Assert.Contains(".kompas-workspace--with-side", minCss, StringComparison.Ordinal);
        Assert.Contains(".competency-match-card__head", minCss, StringComparison.Ordinal);
        Assert.Equal(0, minCss.Count(c => c == '{') - minCss.Count(c => c == '}'));
        Assert.Contains(".kompas-card[hidden]", minCss, StringComparison.Ordinal);
        Assert.Contains("kompas-workspace", home, StringComparison.Ordinal);
        Assert.DoesNotContain("pill-scroller", home, StringComparison.Ordinal);
        var matches = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CompetencyMatchPanel.razor"));
        Assert.Contains("competency-match-card__head", matches, StringComparison.Ordinal);
        Assert.Contains(".competency-skill-list", css, StringComparison.Ordinal);
        Assert.Contains(".competency-skill-list", minCss, StringComparison.Ordinal);

        var profile = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("CandidateKompas", profile, StringComparison.Ordinal);

        var interest = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/CandidateCareerInterestService.cs"));
        Assert.Contains("ResolveCompass", interest, StringComparison.Ordinal);
        Assert.DoesNotContain("CompassJson =", interest, StringComparison.Ordinal);

        var di = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/DependencyInjection.cs"));
        Assert.Contains("ICandidateCareerInterestService", di, StringComparison.Ordinal);
        Assert.Contains("ICandidateCulturePersonalityService", di, StringComparison.Ordinal);
        Assert.Contains("ICareerCompassGenerationService", di, StringComparison.Ordinal);

        var merge = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/DeepAnalysisService.cs"));
        Assert.Contains("ToRiasecScores", merge, StringComparison.Ordinal);
        Assert.Contains("RealisticPercent = riasec.Realistic", merge, StringComparison.Ordinal);
        Assert.Contains("GenerateFromCareerDeepAsync", merge, StringComparison.Ordinal);
        Assert.Contains("CompassJson", merge, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_catalog_covers_general_dutch_occupations_not_only_lobsy_ads()
    {
        Assert.Contains(CareerCompassBuilder.Occupations, o => o.Title.Contains("Verpleegkundige", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(CareerCompassBuilder.Occupations, o => o.Title.Contains("Software", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(CareerCompassBuilder.Occupations, o => o.Title.Contains("Chauffeur", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(CareerCompassBuilder.Occupations, o => o.Title.Contains("Elektricien", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OpenAi_user_prompt_sends_150_answers_without_pii_or_jargon()
    {
        var answers = DeepAnalysisCatalog.CareerQuestions.ToDictionary(q => q.Id, _ => 4);
        var scores = DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career);
        var user = CareerCompassPrompt.User(scores, answers);
        Assert.Contains("150 unieke vragen", user, StringComparison.Ordinal);
        Assert.Contains("Kernfit", user, StringComparison.Ordinal);
        Assert.Contains("→ 4", user, StringComparison.Ordinal);
        Assert.DoesNotContain("@", user, StringComparison.Ordinal);
        Assert.DoesNotContain("gmail", user, StringComparison.OrdinalIgnoreCase);
        AssertNoJargon(user);
        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            Assert.DoesNotContain(code, user, StringComparison.Ordinal);
        }

        Assert.Contains("Nederlandse arbeidsmarkt", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("Jip-en-Janneke", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("superMatches", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("extraversie", CareerCompassPrompt.System, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("neuroticisme", CareerCompassPrompt.System, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("95-100", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("Wat betekent dit voor jou?", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("banenkaart", CareerCompassPrompt.System, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_rebands_openai_jobs_and_drops_jargon_or_weak_matches()
    {
        var json = """
            {
              "strengths": ["Mensen helpen"],
              "superMatches": [],
              "strongChoices": [
                {"title":"Verpleegkundige","percent":97,"why":"Jij wilt voor mensen klaarstaan.","keys":["zorg","verpleeg"]}
              ],
              "broadening": [
                {"title":"RIASEC-coach","percent":90,"why":"Holland-code mismatch.","keys":["coach"]},
                {"title":"Kassamedewerker","percent":70,"why":"Te zwak.","keys":["kassa"]}
              ],
              "practicalNotes": ["Open de banenkaart in Den Haag of het Westland."]
            }
            """;
        var compass = CareerCompassJson.TryDeserialize(json);
        Assert.NotNull(compass);
        Assert.Contains(compass!.SuperMatches, m => m.Title == "Verpleegkundige" && m.Percent == 97);
        Assert.Empty(compass.StrongChoices);
        Assert.DoesNotContain(compass.AllOccupations, m => m.Title.Contains("RIASEC", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(compass.AllOccupations, m => m.Percent < 75);
        Assert.Contains(compass.PracticalNotes, n => n.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
        AssertNoJargon(compass);
    }

    [Fact]
    public void Sanitize_lifts_top_jobs_into_super_match_when_openai_scores_too_low()
    {
        var json = """
            {
              "strengths": ["Aanpakken met je handen"],
              "superMatches": [],
              "strongChoices": [
                {"title":"Medewerker tuinbouw","percent":90,"why":"Jij wilt buiten iets maken.","keys":["kas","tuinbouw"]},
                {"title":"Onderhoudsmonteur","percent":89,"why":"Jij wilt dingen maken en repareren.","keys":["onderhoud","monteur"]},
                {"title":"Magazijnmedewerker","percent":88,"why":"Jij wilt pakken en tillen.","keys":["magazijn","orderpicker"]},
                {"title":"Productiemedewerker","percent":87,"why":"Jij wilt tempo maken.","keys":["productie"]},
                {"title":"Chauffeur","percent":86,"why":"Jij wilt onderweg zijn.","keys":["chauffeur","rijden"]},
                {"title":"Elektricien","percent":85,"why":"Jij wilt installaties aanpakken.","keys":["elektra"]}
              ],
              "broadening": [],
              "practicalNotes": ["Open de banenkaart en filter op hoge match."]
            }
            """;
        var compass = CareerCompassJson.TryDeserialize(json);
        Assert.NotNull(compass);
        Assert.True(compass!.SuperMatches.Count >= 3);
        Assert.All(compass.SuperMatches, m => Assert.InRange(m.Percent, 95, 100));
        Assert.Equal("Medewerker tuinbouw", compass.SuperMatches[0].Title);
        Assert.True(compass.SuperMatches[0].Percent >= compass.SuperMatches[^1].Percent);
        Assert.NotEmpty(compass.StrongChoices);
        Assert.All(compass.StrongChoices, m => Assert.InRange(m.Percent, 85, 94));
        Assert.True(compass.SuperMatches[^1].Percent > compass.StrongChoices[0].Percent);
        AssertNoJargon(compass);
    }

    [Fact]
    public void Compass_json_roundtrip_keeps_openai_flag_and_keys()
    {
        var snapshot = new CareerCompassSnapshot(
            ["Mensen helpen"],
            [new CareerOccupationMatch("Verpleegkundige", 97, CareerCompassBuilder.BandSuper, "Zorg.", ["zorg", "verpleegkundige"])],
            [],
            [],
            ["Open de banenkaart."],
            FromDeepAnalysis: true,
            FromOpenAi: true);
        var back = CareerCompassJson.TryDeserialize(CareerCompassJson.Serialize(snapshot));
        Assert.NotNull(back);
        Assert.True(back!.FromOpenAi);
        Assert.True(back.FromDeepAnalysis);
        Assert.Equal("Verpleegkundige", back.SuperMatches[0].Title);
        Assert.Contains(back.SuperMatches[0].SearchKeys, k => k.Contains("zorg", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Occupation_keys_map_general_title_onto_den_haag_westland_vacancy()
    {
        var occupations = new List<CareerOccupationMatch>
        {
            new("Verpleegkundige", 97, CareerCompassBuilder.BandSuper, "Jij wilt voor mensen klaarstaan.",
                ["verpleegkundige", "zorg", "verpleeg"])
        };
        var hit = VacancyOccupationMatch.TryFit(
            occupations,
            ["Zorg"],
            "Verpleegkundige thuiszorg Den Haag",
            "Zorg in het Westland en Den Haag.");
        Assert.NotNull(hit);
        Assert.Equal("Verpleegkundige", hit!.Value.Title);
        Assert.True(hit.Value.Score01 >= 0.9);

        var miss = VacancyOccupationMatch.TryFit(
            occupations,
            ["IT"],
            "Softwareontwikkelaar Delft",
            "Backend in C#.");
        Assert.Null(miss);

        var generic = VacancyOccupationMatch.TryFit(
            [
                new("Medewerker tuinbouw / kas", 96, CareerCompassBuilder.BandSuper, "Aanpakken.", null)
            ],
            ["Winkel"],
            "Verkoopmedewerker winkel",
            "Kassa en schappen.");
        Assert.Null(generic);
    }

    private static RiasecScores HandsOnScores()
        => new(100, 20, 15, 18, 22, 25);

    private static RiasecScores Peak(string code) => code switch
    {
        CareerTestCatalog.Realistic => new(100, 20, 20, 20, 20, 20),
        CareerTestCatalog.Investigative => new(20, 100, 20, 20, 20, 20),
        CareerTestCatalog.Artistic => new(20, 20, 100, 20, 20, 20),
        CareerTestCatalog.Social => new(20, 20, 20, 100, 20, 20),
        CareerTestCatalog.Enterprising => new(20, 20, 20, 20, 100, 20),
        _ => new(20, 20, 20, 20, 20, 100)
    };

    private static void AssertNoJargon(CareerCompassSnapshot compass)
    {
        foreach (var text in compass.Strengths
                     .Concat(compass.PracticalNotes)
                     .Concat(compass.SuperMatches.SelectMany(OccupationTexts))
                     .Concat(compass.StrongChoices.SelectMany(OccupationTexts))
                     .Concat(compass.Broadening.SelectMany(OccupationTexts)))
        {
            AssertNoJargon(text);
        }
    }

    private static IEnumerable<string> OccupationTexts(CareerOccupationMatch match)
        => [match.Title, match.Why, match.Band, CareerCompassBuilder.BandLabel(match.Band)];

    private static void AssertNoJargon(string text)
        => Assert.False(
            CareerCompassBuilder.ContainsForbiddenJargon(text),
            $"Forbidden jargon in: {text}");

    private static void AssertPdf(byte[] pdf)
    {
        Assert.True(pdf.Length > 500);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    private static byte[] LoadBrandLogo()
    {
        var assembly = typeof(PlatformCompanySettingsService).Assembly;
        using var stream = assembly.GetManifestResourceStream("Jobsy.Infrastructure.Assets.lobsy.png")
            ?? throw new InvalidOperationException("Embedded Lobsy logo missing.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
