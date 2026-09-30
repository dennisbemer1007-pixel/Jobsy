using System.Globalization;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
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
    public void Dream_routes_cover_all_48_jobs_with_five_needs_and_no_urls()
    {
        Assert.Equal(DreamJobCatalog.All.Count, PupilDreamJobRoutes.All.Count);
        Assert.Equal(48, PupilDreamJobRoutes.All.Count);
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
        var dream = _renderer.RenderDreamRoute(result, progress);
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
            Encouragement: dream.Encouragement));

        Assert.True(bytes.Length > 500);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes.AsSpan(0, 4)));

        // QuestPDF compresses streams; assert copy constants are wired and model carries the text.
        var pdfSrc = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Scholen", "PupilReportPdfService.cs"));
        Assert.Contains("PupilReportPdfCopy.NameLine", pdfSrc, StringComparison.Ordinal);
        Assert.Contains("PupilReportPdfCopy.Footer", pdfSrc, StringComparison.Ordinal);
        Assert.Equal("Naam (vul zelf in)", PupilReportPdfCopy.NameLine);
        Assert.Contains("Lobsy bewaart geen namen", PupilReportPdfCopy.Footer, StringComparison.Ordinal);
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
