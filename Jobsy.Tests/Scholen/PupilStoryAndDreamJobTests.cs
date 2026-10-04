using System.Globalization;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

public class PupilStoryAndDreamJobTests
{
    private static readonly string[] Forbidden =
    [
        "collega", "klant", "baas", "leidinggevende", "sollicit", "salaris", "vacature",
        "carrière", "carriere", "werkgever", "kantoor", "instagram", "tiktok", "snapchat",
        "youtube", "vader", "moeder", "niet goed", "zwak", "slecht"
    ];

    private readonly PupilStoryRenderer _renderer = new();
    private static readonly PupilClassContext Groep8 = new(SchoolLevel.Groep78, 8, PupilQuestionSet.Groep78);
    private static readonly PupilClassContext Havo3 = new(SchoolLevel.Havo, 3, PupilQuestionSet.Vo);
    private static readonly PupilClassContext Mix1 = new(SchoolLevel.Mix, 1, PupilQuestionSet.Vo);

    [Fact]
    public void Every_story_and_dream_key_has_nl_text()
    {
        var strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsLeerlingVerhaal.MergeNl(strings);
        Assert.Equal(PupilVerhaalCopy.All.Count, strings.Count);

        foreach (var key in PupilVerhaalCopy.All.Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(PupilVerhaalCopy.Get(key)), key);
        }
    }

    [Fact]
    public void Story_render_is_deterministic_and_tone_safe()
    {
        var result = FixtureResult();
        var progress = new PupilProgress
        {
            PupilCodeId = result.PupilCodeId,
            LikesJson = """["dieren","tekenen","__other__"]""",
            LikeOtherWord = "geheim"
        };
        var keys = PupilStoryTemplates.SelectKeys(result, ["dieren", "tekenen", "__other__"]);
        Assert.DoesNotContain("__other__", keys.LikeChipKeys);
        Assert.Equal(2, keys.LikeChipKeys.Count);

        result.StoryKeysJson = PupilStoryTemplates.Serialize(keys);
        var a = _renderer.Render(result, progress);
        var b = _renderer.Render(result, progress);
        Assert.Equal(a.Body, b.Body);
        Assert.Equal(4, a.Tiles.Count);
        Assert.Equal(4, a.JobIdeas.Count);
        Assert.DoesNotContain("geheim", a.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LeerlingStory.", a.Body, StringComparison.Ordinal);

        foreach (var sentence in a.Body.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.True(words <= 20, $"Too long ({words}): {sentence}");
            var hay = sentence.ToLowerInvariant();
            foreach (var bad in Forbidden)
            {
                Assert.DoesNotContain(bad, hay, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Ai_class_prompts_name_both_letters_and_keep_the_fallback()
    {
        var ai = PupilVerhaalCopy.Get("LeerlingStory.Class.AI.1");
        var ia = PupilVerhaalCopy.Get("LeerlingStory.Class.IA.1");
        Assert.Equal(ai, ia);
        Assert.NotEqual(PupilVerhaalCopy.Get("LeerlingStory.Class.I.1"), ai);
        Assert.NotEqual(PupilVerhaalCopy.Get("LeerlingStory.Class.A.1"), ai);
        Assert.Contains("nieuw", ai, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("werkt", ai, StringComparison.OrdinalIgnoreCase);

        var prompts = _renderer.ClassDiscussionPromptKeys("A", "I");
        Assert.Equal(prompts, _renderer.ClassDiscussionPromptKeys("I", "A"));
        Assert.Contains(prompts, line => line.Contains("nieuw", StringComparison.OrdinalIgnoreCase)
            && line.Contains("werkt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Innovatie_plus_artistic_story_does_not_repeat_bedenkt_graag()
    {
        var paired = FixtureResult();
        paired.CompetenceScoresJson = JsonSerializer.Serialize(new CompetencyScores(40, 40, 40, 90, 40));
        paired.RiasecScoresJson = JsonSerializer.Serialize(new RiasecScores(40, 90, 80, 40, 40, 40));
        paired.HollandCode = "AI";
        paired.StoryKeysJson = "[]";
        var merged = _renderer.Render(paired, null).Body;
        Assert.Contains("Jij bedenkt graag nieuwe manieren en zoekt uit hoe iets werkt.", merged, StringComparison.Ordinal);
        Assert.DoesNotContain("Jij bedenkt graag een nieuwe manier.", merged, StringComparison.Ordinal);

        var artistic = FixtureResult();
        artistic.CompetenceScoresJson = JsonSerializer.Serialize(new CompetencyScores(40, 40, 40, 90, 40));
        artistic.RiasecScoresJson = JsonSerializer.Serialize(new RiasecScores(40, 40, 90, 40, 40, 40));
        artistic.HollandCode = "A";
        artistic.StoryKeysJson = "[]";
        var made = _renderer.Render(artistic, null).Body;
        Assert.Contains("Jij bedenkt graag nieuwe manieren en maakt graag iets moois.", made, StringComparison.Ordinal);
        Assert.DoesNotContain("Jij bedenkt graag een nieuwe manier.", made, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_ordered_riasec_pair_renders_a_sentence()
    {
        const string letters = "RIASEC";
        var canonical = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < letters.Length; i++)
        {
            for (var j = 0; j < letters.Length; j++)
            {
                var suffix = i == j
                    ? letters[i].ToString()
                    : string.Concat(letters[i], letters[j]);
                var text = PupilVerhaalCopy.Get("LeerlingStory.Riasec." + suffix);
                Assert.False(
                    text.StartsWith("LeerlingStory.", StringComparison.Ordinal),
                    suffix + " stayed raw: " + text);
                if (i != j)
                {
                    var swapped = PupilVerhaalCopy.Get(
                        "LeerlingStory.Riasec." + letters[j] + letters[i]);
                    Assert.Equal(text, swapped);
                    canonical.Add(PupilStoryTemplates.CanonicalRiasecPair(letters[i], letters[j]));
                }

                var result = FixtureResult();
                result.HollandCode = suffix;
                result.StoryKeysJson = "[]";
                var view = _renderer.Render(result, null);
                Assert.DoesNotContain("LeerlingStory.", view.Body, StringComparison.Ordinal);
            }
        }

        Assert.Equal(15, canonical.Count);
        foreach (var pair in canonical)
        {
            Assert.True(
                PupilVerhaalCopy.All.ContainsKey("LeerlingStory.Riasec." + pair),
                pair);
        }

        Assert.Equal(
            PupilVerhaalCopy.Get("LeerlingStory.Riasec.R"),
            PupilVerhaalCopy.Get("LeerlingStory.Riasec.RX"));
        Assert.Equal(
            "LeerlingStory.Riasec.CS",
            PupilStoryTemplates.NormalizeRiasecSentenceKey("LeerlingStory.Riasec.SC"));
    }

    [Fact]
    public void Every_pair_keyed_family_resolves_for_all_fifteen_pairs()
    {
        const string letters = "RIASEC";
        var canonical = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < letters.Length; i++)
        {
            for (var j = 0; j < letters.Length; j++)
            {
                var suffix = i == j
                    ? letters[i].ToString()
                    : string.Concat(letters[i], letters[j]);
                if (i != j)
                {
                    canonical.Add(PupilStoryTemplates.CanonicalRiasecPair(letters[i], letters[j]));
                    var swapped = string.Concat(letters[j], letters[i]);
                    AssertPairFamily("LeerlingStory.Riasec.", suffix, swapped, explain: false);
                    AssertPairFamily("LeerlingStory.Tile.Riasec.", suffix, swapped, explain: true);
                    for (var n = 1; n <= 3; n++)
                    {
                        var a = PupilVerhaalCopy.Get($"LeerlingStory.Class.{suffix}.{n}");
                        var b = PupilVerhaalCopy.Get($"LeerlingStory.Class.{swapped}.{n}");
                        AssertResolved(a, suffix);
                        Assert.Equal(a, b);
                    }
                }

                var prompts = _renderer.ClassDiscussionPromptKeys(letters[i].ToString(), letters[j].ToString());
                Assert.Equal(3, prompts.Count);
                foreach (var line in prompts)
                {
                    AssertResolved(line, suffix);
                }

                if (i != j)
                {
                    Assert.Equal(
                        prompts,
                        _renderer.ClassDiscussionPromptKeys(letters[j].ToString(), letters[i].ToString()));
                }
            }
        }

        Assert.Equal(15, canonical.Count);
        foreach (var pair in canonical)
        {
            Assert.True(PupilVerhaalCopy.All.ContainsKey("LeerlingStory.Riasec." + pair), pair);
            Assert.True(PupilVerhaalCopy.All.ContainsKey("LeerlingStory.Tile.Riasec." + pair), pair);
            for (var n = 1; n <= 3; n++)
            {
                Assert.True(PupilVerhaalCopy.All.ContainsKey($"LeerlingStory.Class.{pair}.{n}"), $"{pair}.{n}");
            }
        }

        Assert.Equal(
            PupilVerhaalCopy.Get("LeerlingStory.Class.C.1"),
            PupilVerhaalCopy.Get("LeerlingStory.Class.CX.1"));
        Assert.Equal(
            PupilVerhaalCopy.Get("LeerlingStory.Tile.Riasec.R"),
            PupilVerhaalCopy.Get("LeerlingStory.Tile.Riasec.RX"));
    }

    [Fact]
    public void Every_template_key_resolves_to_text()
    {
        const string letters = "RIASEC";
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < letters.Length; i++)
        {
            for (var j = 0; j < letters.Length; j++)
            {
                var code = i == j
                    ? letters[i].ToString()
                    : string.Concat(letters[i], letters[j]);
                foreach (var value in SchwartzValuesCatalog.CategoryCodes)
                {
                    foreach (var culture in CulturePersonalityCatalog.CultureDimensionCodes)
                    {
                        var result = FixtureResult();
                        result.HollandCode = code;
                        result.TopValue = value;
                        result.TopCulture = culture;
                        var keys = PupilStoryTemplates.SelectKeys(result, ["sport"]);
                        Collect(seen, keys.BigFiveKey, keys.RiasecKey, keys.SchwartzKey, keys.CultureKey);
                        Collect(seen, keys.TileCompetenceKey, keys.TileRiasecKey, keys.TileValueKey, keys.TileCultureKey);
                        Collect(seen, keys.TileCompetenceKey + ".Explain", keys.TileRiasecKey + ".Explain");
                        Collect(seen, keys.TileValueKey + ".Explain", keys.TileCultureKey + ".Explain");
                        foreach (var job in keys.JobIdeaKeys)
                        {
                            seen.Add(job);
                        }
                    }
                }

                var talk = FixtureResult();
                talk.HollandCode = letters[i].ToString();
                talk.TopValue = SchwartzValuesCatalog.CategoryCodes[j % SchwartzValuesCatalog.CategoryCodes.Length];
                foreach (var line in PupilStoryTemplates.ConversationStarterKeys(talk))
                {
                    seen.Add(line);
                }

                foreach (var line in PupilStoryTemplates.ClassDiscussionPromptKeys(
                             letters[i].ToString(),
                             letters[j].ToString()))
                {
                    seen.Add(line);
                }
            }
        }

        Assert.NotEmpty(seen);
        foreach (var key in seen)
        {
            if (!key.StartsWith("LeerlingStory.", StringComparison.Ordinal))
            {
                continue;
            }

            AssertResolved(PupilVerhaalCopy.Get(key), key);
        }
    }

    [Fact]
    public void Value_labels_match_between_tile_sentence_and_school()
    {
        Assert.Equal("Samen met anderen", PupilVerhaalCopy.Get("LeerlingStory.Tile.Val.Connection"));
        Assert.Equal("Iets goed afmaken", PupilVerhaalCopy.Get("LeerlingStory.Tile.Val.Achievement"));
        Assert.Equal(
            PupilVerhaalCopy.Get("LeerlingStory.Tile.Val.Connection"),
            UiStrings.Get("School.Dim.Val.Connection", "nl"));
        Assert.Equal(
            PupilVerhaalCopy.Get("LeerlingStory.Tile.Val.Achievement"),
            UiStrings.Get("School.Dim.Val.Achievement", "nl"));
        Assert.Contains(
            "samen met anderen",
            PupilVerhaalCopy.Get("LeerlingStory.Val.Connection"),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "iets goed afmaken",
            PupilVerhaalCopy.Get("LeerlingStory.Val.Achievement"),
            StringComparison.OrdinalIgnoreCase);
        foreach (var lang in new[] { "en", "pl", "ro", "ar" })
        {
            var text = UiStrings.Get("School.Dim.Val.Connection", lang);
            Assert.NotEqual("Samen met anderen", text);
            Assert.False(string.IsNullOrWhiteSpace(text));
        }
    }

    private static void AssertPairFamily(string prefix, string forward, string backward, bool explain)
    {
        var a = PupilVerhaalCopy.Get(prefix + forward);
        var b = PupilVerhaalCopy.Get(prefix + backward);
        AssertResolved(a, prefix + forward);
        Assert.Equal(a, b);
        if (!explain)
        {
            return;
        }

        var explainA = PupilVerhaalCopy.Get(prefix + forward + ".Explain");
        var explainB = PupilVerhaalCopy.Get(prefix + backward + ".Explain");
        AssertResolved(explainA, prefix + forward + ".Explain");
        Assert.Equal(explainA, explainB);
    }

    private static void AssertResolved(string text, string label)
        => Assert.False(
            text.StartsWith("LeerlingStory.", StringComparison.Ordinal),
            label + " stayed raw: " + text);

    private static void Collect(HashSet<string> seen, params string[] keys)
    {
        foreach (var key in keys)
        {
            seen.Add(key);
        }
    }

    [Fact]
    public void Dream_routes_cover_the_catalog_with_five_needs_and_no_urls()
    {
        Assert.Equal(DreamJobCatalog.All.Count, PupilDreamJobRoutes.All.Count);
        Assert.True(PupilDreamJobRoutes.All.Count >= 58);
        foreach (var job in DreamJobCatalog.All)
        {
            var route = PupilDreamJobRoutes.TryGet(job.Key);
            Assert.NotNull(route);
            Assert.Equal(5, route!.Needs.Count);
            Assert.InRange(route.RouteStepCount, 3, 4);
            foreach (var need in route.Needs)
            {
                Assert.True(PupilVerhaalCopy.TryGet($"LeerlingDroom.Need.{need.NeedKey}", out _), need.NeedKey);
                Assert.True(PupilVerhaalCopy.TryGet($"LeerlingDroom.Need.{need.NeedKey}.Next", out _), need.NeedKey);
            }

            for (var i = 1; i <= route.RouteStepCount; i++)
            {
                Assert.True(PupilVerhaalCopy.TryGet($"LeerlingDroom.Route.{job.Key}.{i}", out var step), $"{job.Key}.{i}");
                AssertNoUrl(step);
                if (i == 1)
                {
                    Assert.StartsWith("Nu: {nu}|", step, StringComparison.Ordinal);
                }

                Assert.DoesNotContain("klas 2", step, StringComparison.Ordinal);
            }

            Assert.True(PupilVerhaalCopy.TryGet($"LeerlingDroom.Route.{job.Key}.Goal", out var goal));
            AssertNoUrl(goal);
            if (route.HasAltRoute)
            {
                Assert.True(PupilVerhaalCopy.TryGet($"LeerlingDroom.Alt.{job.Key}", out var alt));
                AssertNoUrl(alt);
            }
        }
    }

    [Fact]
    public void NowLabel_matches_level_and_year()
    {
        Assert.Equal("groep 8", PupilDreamJobRoutes.NowLabel(Groep8));
        Assert.Equal("groep 7", PupilDreamJobRoutes.NowLabel(new(SchoolLevel.Groep78, 7, PupilQuestionSet.Groep78)));
        Assert.Equal("klas 3 havo", PupilDreamJobRoutes.NowLabel(Havo3));
        Assert.Equal("klas 1", PupilDreamJobRoutes.NowLabel(Mix1));
        Assert.Equal("klas 2 vmbo", PupilDreamJobRoutes.NowLabel(new(SchoolLevel.VmboB, 2, PupilQuestionSet.Vo)));
        Assert.Equal("klas 4 mavo", PupilDreamJobRoutes.NowLabel(new(SchoolLevel.Mavo, 4, PupilQuestionSet.Vo)));
        Assert.Equal("klas 5 vwo", PupilDreamJobRoutes.NowLabel(new(SchoolLevel.Vwo, 5, PupilQuestionSet.Vo)));
        Assert.Equal("klas 1", PupilDreamJobRoutes.NowLabel(new(SchoolLevel.Anders, 1, PupilQuestionSet.Vo)));
    }

    [Fact]
    public void Every_route_renders_for_groep8_havo3_and_mix1_without_placeholders()
    {
        var result = FixtureResult();
        var progress = new PupilProgress { PupilCodeId = result.PupilCodeId, LikesJson = "[]" };
        foreach (var job in DreamJobCatalog.All)
        {
            result.DreamJobKey = job.Key;
            progress.DreamJobKey = job.Key;
            foreach (var ctx in new[] { Groep8, Havo3, Mix1 })
            {
                var dream = _renderer.RenderDreamRoute(result, progress, ctx);
                Assert.NotEmpty(dream.RouteSteps);
                Assert.StartsWith(
                    "Nu: " + PupilDreamJobRoutes.NowLabel(ctx) + " — ",
                    dream.RouteSteps[0],
                    StringComparison.Ordinal);
                var blob = string.Join(" ", dream.RouteSteps.Concat(dream.HaveItems).Concat(dream.LearnItems)
                    .Append(dream.Encouragement ?? "").Append(dream.AltRoute ?? ""));
                Assert.DoesNotContain("{", blob, StringComparison.Ordinal);
                Assert.DoesNotContain("{nu}", blob, StringComparison.Ordinal);
                if (ctx.Year != 2)
                {
                    Assert.DoesNotContain("klas 2", blob, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Vo_copy_uses_docent_of_mentor_g78_keeps_leraar()
    {
        Assert.Contains("leraar", PupilVerhaalCopy.Get("LeerlingDroom.UndecidedHint", PupilQuestionSet.Groep78), StringComparison.Ordinal);
        Assert.Contains("docent of mentor", PupilVerhaalCopy.Get("LeerlingDroom.UndecidedHint", PupilQuestionSet.Vo), StringComparison.Ordinal);
        Assert.DoesNotContain("leraar", PupilVerhaalCopy.Get("LeerlingDroom.UndecidedHint", PupilQuestionSet.Vo), StringComparison.Ordinal);
        Assert.Contains("docent of mentor", PupilVerhaalCopy.Get("LeerlingDroom.Cheer.Low", PupilQuestionSet.Vo), StringComparison.Ordinal);
        Assert.Contains("leraar", PupilVerhaalCopy.Get("LeerlingDroom.Cheer.Low", PupilQuestionSet.Groep78), StringComparison.Ordinal);
        Assert.Contains("mentor", PupilVerhaalCopy.Get("LeerlingDroom.Need.Ruimte.Next", PupilQuestionSet.Vo), StringComparison.Ordinal);
        Assert.Contains("leraar", PupilVerhaalCopy.Get("LeerlingDroom.Need.Ruimte.Next", PupilQuestionSet.Groep78), StringComparison.Ordinal);

        var low = FixtureResult(competence: 10, riasec: 10, values: 10);
        low.DreamJobKey = "piloot";
        var progress = new PupilProgress { PupilCodeId = low.PupilCodeId, DreamJobKey = "piloot", LikesJson = "[]" };
        var vo = _renderer.RenderDreamRoute(low, progress, Havo3);
        var g78 = _renderer.RenderDreamRoute(low, progress, Groep8);
        Assert.Contains("docent of mentor", vo.Encouragement ?? "", StringComparison.Ordinal);
        Assert.Contains("leraar", g78.Encouragement ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("leraar", vo.Encouragement ?? "", StringComparison.Ordinal);
        Assert.Contains("mentor", string.Join(" ", vo.LearnItems), StringComparison.Ordinal);
    }

    [Fact]
    public void Fit_evaluator_is_deterministic_across_catalog_and_has_none_met_fallback()
    {
        var low = FixtureResult(competence: 10, riasec: 10, values: 10);
        var progress = new PupilProgress { PupilCodeId = low.PupilCodeId, LikesJson = "[]" };
        foreach (var job in DreamJobCatalog.All)
        {
            var a = PupilDreamJobFit.Evaluate(low, progress, job.Key);
            var b = PupilDreamJobFit.Evaluate(low, progress, job.Key);
            Assert.NotNull(a);
            Assert.Equal(a!.HaveCount, b!.HaveCount);
            Assert.Equal(a.Snapshot.Met, b.Snapshot.Met);
            Assert.Contains(PupilDreamJobFit.GenericHaveKey, a.HaveSentenceKeys);
        }

        var high = FixtureResult(competence: 90, riasec: 90, values: 90);
        var progressHigh = new PupilProgress
        {
            PupilCodeId = high.PupilCodeId,
            LikesJson = JsonSerializer.Serialize(new[]
            {
                "dieren", "techniek", "koken", "sport", "muziek", "tekenen", "computers",
                "bouwen", "kleine-kinderen", "natuur", "programmeren", "rekenen", "gamen"
            })
        };
        var fit = PupilDreamJobFit.Evaluate(high, progressHigh, "dierenarts");
        Assert.NotNull(fit);
        Assert.True(fit!.HaveCount >= 1);
        Assert.DoesNotContain(PupilDreamJobFit.GenericHaveKey, fit.HaveSentenceKeys);
    }

    [Fact]
    public void Unknown_dream_job_key_is_rejected()
    {
        Assert.False(PupilDreamJobFit.IsKnownJobKey("geen-bestaand-beroep"));
        Assert.True(PupilDreamJobFit.IsKnownJobKey("dierenarts"));
        Assert.True(PupilDreamJobFit.IsKnownJobKey(PupilDreamJobFit.UndecidedKey));
    }

    [Fact]
    public void Pdf_contains_story_name_line_and_privacy_footer_and_does_not_use_blob()
    {
        var result = FixtureResult();
        var keys = PupilStoryTemplates.SelectKeys(result, ["dieren", "tekenen"]);
        result.StoryKeysJson = PupilStoryTemplates.Serialize(keys);
        result.DreamJobKey = "dierenarts";
        var progress = new PupilProgress
        {
            PupilCodeId = result.PupilCodeId,
            LikesJson = """["dieren","tekenen"]""",
            DislikesJson = """["voor-de-klas"]""",
            DreamJobKey = "dierenarts"
        };
        var story = _renderer.Render(result, progress);
        var dream = _renderer.RenderDreamRoute(result, progress, Groep8);
        var footer = PupilVerhaalCopy.Get("LeerlingPdf.Footer", Groep8);
        var pdf = new PupilReportPdfService();
        var bytes = pdf.Render(new PupilReportPdfModel(
            SchoolName: "Voorbeeld College",
            ClassName: "2B",
            DisplayCode: "K7Q-M2P",
            Date: new DateOnly(2026, 9, 29),
            StoryBody: story.Body,
            Tiles: story.Tiles.Select(t => (t.KidLabel, t.Explanation)).ToList(),
            Likes: ["Dieren", "Tekenen"],
            Dislikes: ["Voor de klas praten"],
            JobIdeas: story.JobIdeas,
            DreamJobTitle: dream.JobTitle,
            RouteSteps: dream.RouteSteps,
            Encouragement: dream.Encouragement,
            Footer: footer));

        Assert.True(bytes.Length > 500);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes.AsSpan(0, 4)));

        // QuestPDF compresses streams; assert copy constants are wired and model carries the text.
        var pdfSrc = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Scholen", "PupilReportPdfService.cs"));
        Assert.Contains("PupilReportPdfCopy.NameLine", pdfSrc, StringComparison.Ordinal);
        Assert.Contains("PupilReportPdfCopy.Footer", pdfSrc, StringComparison.Ordinal);
        Assert.Equal("Naam (vul zelf in)", PupilReportPdfCopy.NameLine);
        Assert.Contains("Lobsy bewaart geen namen", PupilReportPdfCopy.Footer, StringComparison.Ordinal);
        Assert.Contains("leraar", footer, StringComparison.Ordinal);
        Assert.Contains("docent", PupilVerhaalCopy.Get("LeerlingPdf.Footer", Havo3), StringComparison.Ordinal);
        Assert.Contains("Nu: groep 8", dream.RouteSteps[0], StringComparison.Ordinal);
        Assert.DoesNotContain("{nu}", dream.RouteSteps[0], StringComparison.Ordinal);
        Assert.Contains("Belangrijk voor jou", story.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(
            typeof(PupilReportPdfService).GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.FullName ?? p.ParameterType.Name),
            n => n.Contains("Blob", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found");
    }

    private static void AssertNoUrl(string text)
    {
        Assert.DoesNotContain("http", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("www", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", text, StringComparison.Ordinal);
    }

    private static PupilResult FixtureResult(int competence = 70, int riasec = 70, int values = 70)
    {
        var id = Guid.NewGuid();
        return new PupilResult
        {
            PupilCodeId = id,
            SchoolClassId = Guid.NewGuid(),
            CompletedAtUtc = DateTime.UtcNow,
            CompetenceScoresJson = JsonSerializer.Serialize(new CompetencyScores(competence, competence, competence, competence, competence)),
            RiasecScoresJson = JsonSerializer.Serialize(new RiasecScores(riasec, riasec, 40, riasec, 40, 40)),
            HollandCode = "RS",
            ValuesScoresJson = JsonSerializer.Serialize(new SchwartzValuesScores(values, values, 40, 40, values)),
            TopValue = SchwartzValuesCatalog.Connection,
            CultureScoresJson = JsonSerializer.Serialize(new CulturePersonalityScores(
                Autonomy: 40, Informal: 40, Collaboration: 40, Flexibility: 40, Innovation: 40, PeopleFirst: 80)),
            TopCulture = CulturePersonalityCatalog.PeopleFirst,
            ScoringVersion = "1",
            StoryTemplateVersion = "1",
            StoryKeysJson = "[]"
        };
    }
}

/// <summary>
/// Keeps <c>docs/scholen/droombanen-leerlingen.md</c> in sync.
/// Regenerate: JOBSY_UPDATE_PUPIL_DREAM_DOC=1 dotnet test --filter FullyQualifiedName~PupilDreamJobDocFreshness
/// </summary>
public class PupilDreamJobDocFreshnessTests
{
    public const string RelativeDocPath = "docs/scholen/droombanen-leerlingen.md";

    [Fact]
    public void Droombanen_doc_matches_catalog()
    {
        var generated = Normalize(Generate());
        var root = FindRepoRoot();
        var path = Path.Combine(root, RelativeDocPath);
        var update = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_UPDATE_PUPIL_DREAM_DOC"),
            "1",
            StringComparison.Ordinal);

        if (update || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, generated);
        }

        Assert.True(File.Exists(path), $"Missing {RelativeDocPath}");
        var onDisk = Normalize(File.ReadAllText(path));
        Assert.True(
            string.Equals(generated, onDisk, StringComparison.Ordinal),
            $"{RelativeDocPath} is outdated. Regenerate with JOBSY_UPDATE_PUPIL_DREAM_DOC=1");
    }

    public static string Generate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Droombanen leerlingen (review)");
        sb.AppendLine();
        sb.AppendLine("Gegenereerd uit `PupilDreamJobRoutes` + `PupilVerhaalCopy`. Wijzig de bronnen, niet dit bestand met de hand.");
        sb.AppendLine();
        foreach (var route in PupilDreamJobRoutes.All)
        {
            var title = DreamJobCatalog.All.First(j => j.Key == route.JobKey).TitleNl;
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"## {title} (`{route.JobKey}`)"));
            sb.AppendLine();
            sb.AppendLine("Needs:");
            foreach (var need in route.Needs)
            {
                var have = PupilVerhaalCopy.Get($"LeerlingDroom.Need.{need.NeedKey}");
                var next = PupilVerhaalCopy.Get($"LeerlingDroom.Need.{need.NeedKey}.Next");
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"- `{need.NeedKey}` ({need.Signal} ≥ {need.Threshold}): {have} / {next}"));
            }

            sb.AppendLine();
            sb.AppendLine("Route:");
            for (var i = 1; i <= route.RouteStepCount; i++)
            {
                sb.AppendLine($"- {PupilVerhaalCopy.Get($"LeerlingDroom.Route.{route.JobKey}.{i}")}");
            }

            sb.AppendLine($"- Goal: {PupilVerhaalCopy.Get($"LeerlingDroom.Route.{route.JobKey}.Goal")}");
            if (route.HasAltRoute)
            {
                sb.AppendLine($"- Alt: {PupilVerhaalCopy.Get($"LeerlingDroom.Alt.{route.JobKey}")}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Normalize(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
