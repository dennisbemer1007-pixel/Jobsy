using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jobsy.Core.Careers;
using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.ProfileSections;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public class OccupationOutlookTests
{
    private const string ClerkId = "6c999fc7-c6b7-4ef3-a4b9-af124a1783a2";
    private const string CleanerId = "303a1e34-cb16-4054-b323-81e5eec17397";

    private static readonly string[] Forbidden =
    [
        "verdwijnt", "verdwijnen", "geen toekomst", "overbodig", "ontslag", "werkloos"
    ];

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Coverage_matches_the_pinned_sources()
    {
        var outlook = LoadOutlook();
        var coverage = outlook["coverage"]!;
        Assert.Equal(2956, coverage["ilo"]!.GetValue<int>());
        Assert.Equal(3039, coverage["roaItkbAndOpenings"]!.GetValue<int>());
        Assert.Equal(2612, coverage["roaDirection"]!.GetValue<int>());
        Assert.Equal("1 september 2026", outlook["peildatum"]!.GetValue<string>());

        var tasks = JsonSerializer.Deserialize<List<TaskRow>>(File.ReadAllText(TasksPath()), Json) ?? [];
        Assert.Equal(1657, tasks.Count);
        Assert.All(tasks, row =>
        {
            Assert.Equal("concept, wacht op akkoord", row.Status);
            Assert.False(string.IsNullOrWhiteSpace(row.Nl));
            Assert.False(string.IsNullOrWhiteSpace(row.En));
        });
    }

    [Theory]
    [InlineData("zeer groot")]
    [InlineData("groot")]
    [InlineData("enige")]
    [InlineData("vrijwel geen")]
    [InlineData("geen")]
    public void Each_itkb_typering_uses_its_sentence(string typering)
    {
        var line = OccupationOutlook.Demand(Group(typering, openings: null));
        if (typering is "zeer groot" or "groot")
        {
            Assert.StartsWith("Tot 2030 hebben werkgevers naar verwachting moeite", line);
        }
        else if (typering == "enige")
        {
            Assert.StartsWith("Tot 2030 is er naar verwachting redelijk wat vraag", line);
        }
        else
        {
            Assert.Contains("Een baan vinden kan meer moeite kosten.", line);
        }

        Assert.DoesNotContain("van elke 100", line);
    }

    [Fact]
    public void Openings_sentence_uses_replacement_or_expansion()
    {
        var replacement = OccupationOutlook.Demand(Group("enige", 34, replacement: 31, expansion: 3));
        Assert.Contains("34 van elke 100 banen vrij, vooral omdat mensen met pensioen gaan of ander werk kiezen.", replacement);

        var expansion = OccupationOutlook.Demand(Group("groot", 20, replacement: 4, expansion: 16));
        Assert.Contains("20 van elke 100 banen vrij, vooral omdat er meer werk bijkomt.", expansion);
    }

    [Theory]
    [InlineData("Not Exposed", "Volgens de ILO verandert AI weinig aan de taken in dit werk.")]
    [InlineData("Minimal Exposure", "AI raakt maar een klein deel van de taken.")]
    [InlineData("Exposed: Gradient 1", "Een paar taken kunnen door computers of AI veranderen. Het meeste werk blijft mensenwerk.")]
    [InlineData("Exposed: Gradient 2", "Een deel van de taken kan door AI veranderen.")]
    [InlineData("Exposed: Gradient 3", "Veel taken kunnen door AI veranderen. Het werk gaat er waarschijnlijk anders uitzien.")]
    [InlineData("Exposed: Gradient 4", "De meeste taken kunnen door AI veranderen. Het is slim om nu iets extra's te leren.")]
    [InlineData("", "Dat weten we niet: de ILO heeft geen cijfers voor dit beroep.")]
    public void Each_potential25_uses_the_sentence_without_examples(string potential, string expected)
        => Assert.Equal(expected, OccupationOutlook.Ai(potential, null, null));

    [Fact]
    public void Approved_task_text_appears_and_concept_text_does_not()
    {
        var plain = OccupationOutlook.Shared.Get(ClerkId);
        Assert.Equal(
            "De meeste taken kunnen door AI veranderen. Het is slim om nu iets extra's te leren.",
            plain.AiLine);
        Assert.Empty(plain.ChangeTasks);
        Assert.DoesNotContain("Transcribing", plain.AiLine);

        var approved = LoadWithApprovedClerkTask();
        Assert.Contains("Gegevens invoeren", approved.AiLine);
        Assert.Contains("Gegevens invoeren", approved.ChangeTasks);
    }

    [Fact]
    public void Golden_outlook_without_approved_translations()
    {
        var clerk = OccupationOutlook.Shared.Get(ClerkId);
        Assert.Equal(
            "Tot 2030 is er naar verwachting redelijk wat vraag naar mensen voor dit soort werk. Tot 2030 komen naar schatting 34 van elke 100 banen vrij, vooral omdat mensen met pensioen gaan of ander werk kiezen.",
            clerk.DemandLine);
        Assert.Equal(
            "De meeste taken kunnen door AI veranderen. Het is slim om nu iets extra's te leren.",
            clerk.AiLine);
        Assert.Contains("peildatum 1 september 2026", clerk.Sources[0]);

        var cleaner = OccupationOutlook.Shared.Get(CleanerId);
        Assert.Equal(
            "Tot 2030 hebben werkgevers naar verwachting moeite om genoeg mensen te vinden voor dit soort werk. Tot 2030 komen naar schatting 35 van elke 100 banen vrij, vooral omdat mensen met pensioen gaan of ander werk kiezen.",
            cleaner.DemandLine);
        Assert.Equal(
            "Volgens de ILO verandert AI weinig aan de taken in dit werk.",
            cleaner.AiLine);
    }

    [Fact]
    public void Missing_ilo_and_missing_demand_say_we_do_not_know()
    {
        var missingIlo = OccupationCatalog.Shared.All.First(job => job.Isco == "3435");
        var outlook = OccupationOutlook.Shared.Get(missingIlo.Id);
        Assert.Equal(OccupationOutlook.MissingIlo, outlook.AiLine);

        Assert.Equal(
            OccupationOutlook.MissingDemand,
            OccupationOutlook.Demand(null));
    }

    [Fact]
    public void No_outlook_or_tip_uses_a_forbidden_word()
    {
        var scores = new RiasecScores(40, 30, 20, 50, 35, 80);
        foreach (var job in OccupationCatalog.Shared.All)
        {
            var outlook = OccupationOutlook.Shared.Get(job.Id);
            AssertClean(outlook.DemandLine);
            AssertClean(outlook.AiLine);
            AssertClean(string.Join(" ", outlook.Sources));
            var tip = CurrentJobOutlook.TryCreate(
                new CandidateEmployerHistoryDto("Werk", "Rol", StartMonth: "2024-01", EscoId: job.Id),
                scores,
                "mbo 4");
            if (tip is null)
            {
                continue;
            }

            AssertClean(tip.Outlook.DemandLine);
            AssertClean(tip.Outlook.AiLine);
            AssertClean(tip.NoAdjacentMessage);
            Assert.True(tip.Adjacent.Count <= 3);
            foreach (var skill in tip.SkillsToLearn)
            {
                AssertClean(skill);
            }
        }
    }

    [Fact]
    public void Current_job_tip_needs_a_confirmed_esco_id()
    {
        var scores = new RiasecScores(20, 20, 20, 40, 30, 90);
        Assert.Null(CurrentJobOutlook.TryCreate(
            new CandidateEmployerHistoryDto("Werk", "Kantoor", IsCurrent: true),
            scores,
            null));
        Assert.Null(CurrentJobOutlook.TryCreate(
            new CandidateEmployerHistoryDto("Werk", "Kantoor", StartMonth: "2020-01", EndMonth: "2024-01", EscoId: ClerkId),
            scores,
            null));

        var tip = CurrentJobOutlook.TryCreate(
            new CandidateEmployerHistoryDto("Werk", "Kantoor", IsCurrent: true, EscoId: ClerkId),
            scores,
            "mbo 4");
        Assert.NotNull(tip);
        Assert.True(tip!.Adjacent.Count <= 3);
        AssertEachAdjacent(tip, scores, "mbo 4");
        if (tip.Adjacent.Count == 0)
        {
            Assert.Equal(CurrentJobOutlook.NoAdjacentSentence, tip.NoAdjacentMessage);
            Assert.Empty(tip.SkillsToLearn);
        }
    }

    [Fact]
    public void At_least_one_current_job_has_a_sourced_neighbour()
    {
        var scores = new RiasecScores(70, 40, 25, 55, 45, 80);
        var found = OccupationCatalog.Shared.All.Any(job =>
        {
            var tip = CurrentJobOutlook.TryCreate(
                new CandidateEmployerHistoryDto("Werk", job.Nl, IsCurrent: true, EscoId: job.Id),
                scores,
                "hbo");
            return tip is { Adjacent.Count: > 0 };
        });
        Assert.True(found);
    }

    [Fact]
    public void Skills_come_only_from_the_shown_neighbours()
    {
        var scores = new RiasecScores(70, 40, 25, 55, 45, 80);
        foreach (var job in OccupationCatalog.Shared.Listable.Take(80))
        {
            var tip = CurrentJobOutlook.TryCreate(
                new CandidateEmployerHistoryDto("Werk", job.Nl, StartMonth: "2023-05", EscoId: job.Id),
                scores,
                "hbo");
            if (tip is null || tip.Adjacent.Count == 0)
            {
                continue;
            }

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var adjacent in tip.Adjacent)
            {
                foreach (var index in OccupationSkills.Shared.Essential(adjacent.EscoId))
                {
                    var label = OccupationSkills.Shared.Label(index);
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        allowed.Add(label);
                    }
                }
            }

            Assert.InRange(tip.SkillsToLearn.Count, 0, 5);
            Assert.All(tip.SkillsToLearn, skill => Assert.Contains(skill, allowed));
            var current = OccupationSkills.Shared.AllOf(job.Id);
            Assert.All(tip.SkillsToLearn, skill =>
            {
                var index = IndexOf(skill);
                Assert.True(index < 0 || !current.Contains(index));
            });
            return;
        }

        Assert.Fail("No sampled job produced a neighbour, so the skill rule was not exercised.");
    }

    [Fact]
    public void Employer_history_round_trips_with_and_without_esco_id()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var stored = JsonSerializer.Serialize(
            new CandidateEmployerHistoryDto("Bakkerij", "Kantoor", IsCurrent: true, EscoId: ClerkId),
            options);
        var back = JsonSerializer.Deserialize<CandidateEmployerHistoryDto>(stored, options);
        Assert.Equal(ClerkId, back!.EscoId);

        var legacy = JsonSerializer.Deserialize<CandidateEmployerHistoryDto>(
            """{"employerName":"Bakkerij","role":"Kantoor","isCurrent":true}""",
            options);
        Assert.Null(legacy!.EscoId);
        Assert.Equal("Bakkerij", legacy.EmployerName);
    }

    [Fact]
    public void ParsePreferences_keeps_a_real_esco_id_and_drops_an_unknown_one()
    {
        var kept = Jobsy.Api.Controllers.MeController.ParsePreferences(
            $$"""{"employers":[{"employerName":"Werk","role":"Kantoor","isCurrent":true,"escoId":"{{ClerkId}}"}]}""");
        Assert.Equal(ClerkId, kept.Employers!.Single().EscoId);

        var dropped = Jobsy.Api.Controllers.MeController.ParsePreferences(
            """{"employers":[{"employerName":"Werk","role":"Kantoor","escoId":"not-a-real-id"}]}""");
        Assert.Null(dropped.Employers!.Single().EscoId);

        var old = Jobsy.Api.Controllers.MeController.ParsePreferences(
            """{"employers":[{"employerName":"Werk","role":"Kantoor"}]}""");
        Assert.Null(old.Employers!.Single().EscoId);

        var noRole = Jobsy.Api.Controllers.MeController.ParsePreferences(
            $$"""{"employers":[{"employerName":"Werk","escoId":"{{ClerkId}}"}]}""");
        Assert.Null(noRole.Employers!.Single().EscoId);
    }

    [Fact]
    public void Changing_the_role_clears_the_confirmed_occupation()
    {
        var editor = new CandidateProfileEditor(null!, null!, null!, null!, null!);
        editor.Employers.Add(new CandidateEmployerHistory
        {
            EmployerName = "Werk",
            Role = "Kantoor",
            EscoId = ClerkId,
            OccupationDismissed = true
        });
        editor.SetEmployerRole(0, "Kantoor");
        Assert.Equal(ClerkId, editor.Employers[0].EscoId);

        editor.SetEmployerRole(0, "Schoonmaker");
        Assert.Null(editor.Employers[0].EscoId);
        Assert.False(editor.Employers[0].OccupationDismissed);
    }

    [Fact]
    public void Cv_extraction_never_sets_esco_id()
    {
        var source = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Infrastructure/Services/CvExtractionService.cs"));
        Assert.Contains("EscoId = null", source, StringComparison.Ordinal);
        Assert.DoesNotContain("e.EscoId", source, StringComparison.Ordinal);
        var merged = CvProfileMerge.Apply(
            null,
            null,
            null,
            new CandidatePreferencesDto([], null, null, Employers:
            [
                new CandidateEmployerHistoryDto("Bestaand", "Kantoor", EscoId: ClerkId)
            ]),
            new CvExtractedProfile(null, null, null, null, null, null, null,
                [new CandidateEmployerHistoryDto("Nieuw", "Schoonmaker")]));
        Assert.Equal(ClerkId, merged.Preferences.Employers!.Single(e => e.EmployerName == "Bestaand").EscoId);
        Assert.Null(merged.Preferences.Employers!.Single(e => e.EmployerName == "Nieuw").EscoId);
    }

    [Fact]
    public void Fact_guard_rejects_an_outlook_that_was_not_produced()
    {
        var outlook = OccupationOutlook.Shared.Get(ClerkId);
        var sheet = CandidateFactSheet.Personal([], [], [], [outlook.DemandLine!]);
        sheet.RememberOutlook([outlook.DemandLine!, outlook.AiLine!]);
        Assert.Null(CandidateFactGuard.RejectionReason(outlook.DemandLine, sheet));
        Assert.Equal(
            "unknown-outlook",
            CandidateFactGuard.RejectionReason("Tot 2030 verdwijnt dit werk door AI.", sheet));
        Assert.Equal(
            "unknown-job",
            CandidateFactGuard.RejectionReason(
                "Je kunt ook astronaut worden.",
                CandidateFactSheet.Personal([], [], [], ["kantoorbediende"], checkJobTitles: true)));
    }

    [Fact]
    public void Candidate_pages_show_the_outlook_and_the_confirm_step()
    {
        var root = TestRepo.FindRoot();
        var career = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/CareerCompassPanel.razor"));
        var fit = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/RoleFitCheckPanel.razor"));
        var experience = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/ProfileSections/ExperienceSection.razor"));
        var bronnen = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Legal/Bronnen.razor"));
        Assert.Contains("OccupationOutlookBlock", career, StringComparison.Ordinal);
        Assert.Contains("OccupationOutlookBlock", fit, StringComparison.Ordinal);
        Assert.Contains("Outlook.AskWork", experience, StringComparison.Ordinal);
        Assert.Contains("Outlook.YesThis", experience, StringComparison.Ordinal);
        Assert.Contains("CurrentJobTipBlock", experience, StringComparison.Ordinal);
        Assert.Contains("https://doi.org/10.34894/DVQTOG", bronnen, StringComparison.Ordinal);
        Assert.Contains("should not be considered an official ILO adaptation", bronnen, StringComparison.Ordinal);
        Assert.Contains("Vaardigheden bij een beroep komen uit dezelfde ESCO-lijst.", bronnen, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ISCO")]
    [InlineData("ESCO")]
    [InlineData("O*NET")]
    [InlineData("gradient")]
    [InlineData("RIASEC")]
    public void Jargon_stays_out_of_candidate_text(string term)
        => Assert.True(CareerCompassBuilder.ContainsForbiddenJargon(term));

    private static void AssertEachAdjacent(CurrentJobTip tip, RiasecScores scores, string education)
    {
        var current = OccupationCatalog.Shared.Get(tip.EscoId)!;
        Assert.True(OccupationOutlook.Shared.TryGetComparison(current.Id, out var currentIlo, out var currentItkb));
        var gate = CareerEducationGate.MaxIscoLevel(education);
        var allowLead = CareerCompassBuilder.EnterprisingInTop3(scores);
        foreach (var job in tip.Adjacent)
        {
            var occupation = OccupationCatalog.Shared.Get(job.EscoId);
            Assert.NotNull(occupation);
            Assert.True(occupation!.IsListable);
            Assert.True(OccupationOutlook.Shared.TryGetComparison(job.EscoId, out var ilo, out var itkb));
            Assert.True(ilo <= currentIlo);
            Assert.True(itkb >= currentItkb);
            Assert.True(ilo < currentIlo || itkb > currentItkb);
            Assert.True(occupation.IscoLevel <= current.IscoLevel + 1);
            Assert.True(CareerEducationGate.Passes(occupation.IscoLevel, gate));
            if (!allowLead)
            {
                Assert.False(OccupationCatalog.IsLeadership(occupation));
            }
        }
    }

    private static OccupationOutlookResult LoadWithApprovedClerkTask()
    {
        var outlook = LoadOutlook();
        var high = outlook["iloByIsco"]!["4110"]!["high"]![0]!;
        var taskId = high["taskID"]!.GetValue<string>();
        var tasks = JsonSerializer.Serialize(new[]
        {
            new TaskRow("4110", taskId, high["en"]!.GetValue<string>(), "Gegevens invoeren", OccupationOutlook.ApprovedStatus, "", "")
        }, Json);
        using var outlookStream = File.OpenRead(Path.Combine(TestRepo.FindRoot(), "Jobsy.Core/Data/Occupations/outlook.json"));
        using var taskStream = new MemoryStream(Encoding.UTF8.GetBytes(tasks));
        return OccupationOutlook.Load(outlookStream, taskStream).Get(ClerkId);
    }

    private static JsonNode LoadOutlook()
        => JsonNode.Parse(File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Core/Data/Occupations/outlook.json")))!;

    private static string TasksPath()
        => Path.Combine(TestRepo.FindRoot(), "tools/occupations/ilo_tasks_nl.json");

    private static int IndexOf(string label)
    {
        var skills = JsonSerializer.Deserialize<List<SkillRow>>(
            File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Core/Data/Occupations/skills.nl.json")),
            Json);
        return skills?.FindIndex(row => string.Equals(row.Nl, label, StringComparison.OrdinalIgnoreCase)) ?? -1;
    }

    private static RoaGroup Group(string typering, double? openings, double? replacement = null, double? expansion = null)
        => new()
        {
            Itkb = new RoaMeasure { Typering = typering },
            Baanopeningen = openings is null ? null : new RoaMeasure { Totaal6jrperc = openings },
            Vervanging = replacement is null ? null : new RoaMeasure { Totaal6jrperc = replacement },
            Uitbreiding = expansion is null ? null : new RoaMeasure { Totaal6jrperc = expansion }
        };

    private static void AssertClean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var fold = text.ToLowerInvariant();
        foreach (var word in Forbidden)
        {
            Assert.DoesNotContain(word, fold);
        }
    }

    private sealed record TaskRow(
        string Isco,
        [property: System.Text.Json.Serialization.JsonPropertyName("taskID")] string TaskId,
        string En,
        string Nl,
        string Status,
        string? ReviewedBy,
        string? ReviewedOn)
    {
        public TaskRow() : this("", "", "", "", "", "", "")
        {
        }
    }

    private sealed record SkillRow(string? Uri, string? Nl);
}
