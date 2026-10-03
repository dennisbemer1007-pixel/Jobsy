using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Freezes pupil progress/answer DTO shapes across a full 60-item + island run.
/// Baseline was recorded on the 03a base commit before the registry/flow refactor.
/// Regenerate with JOBSY_UPDATE_PUPIL_FLOW_GOLDEN=1 only when intentional.
/// </summary>
public class PupilFlowGoldenTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private const string RelativeBaseline = "Jobsy.Tests/Baselines/pupil-flow-golden.json";
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        WriteIndented = true
    };

    public PupilFlowGoldenTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Vo_legacy_flow_matches_pre_refactor_golden_snapshot()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync(SchoolLevel.Havo, 2, PupilQuestionSet.Vo);
        var snapshot = await RunFlowAsync(seed);
        AssertMatchesBaseline(snapshot, "vo-legacy");
    }

    [Fact]
    public async Task Groep78_flow_matches_pre_refactor_golden_snapshot()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync(SchoolLevel.Groep78, 8, PupilQuestionSet.Groep78);
        var snapshot = await RunFlowAsync(seed);
        AssertMatchesBaseline(snapshot, "groep78");
    }

    private void AssertMatchesBaseline(GoldenFlowSnapshot snapshot, string label)
    {
        var json = SerializeBaselineProjection(snapshot);
        var root = FindRepoRoot();
        var path = Path.Combine(root, RelativeBaseline);
        var update = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_UPDATE_PUPIL_FLOW_GOLDEN"),
            "1",
            StringComparison.Ordinal);

        if (update || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        Assert.True(File.Exists(path), $"Missing {RelativeBaseline}");
        var onDisk = Normalize(File.ReadAllText(path));
        Assert.True(
            string.Equals(Normalize(json), onDisk, StringComparison.Ordinal),
            $"{RelativeBaseline} differs for {label}. " +
            "If intentional: JOBSY_UPDATE_PUPIL_FLOW_GOLDEN=1 dotnet test --filter FullyQualifiedName~PupilFlowGolden");
    }

    private async Task<GoldenFlowSnapshot> RunFlowAsync(SeedInfo seed)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var login = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var bank = new PupilQuestionBank();
        var frames = new List<GoldenFrame>();

        var progress0 = await client.GetFromJsonAsync<JsonObject>("api/pupil/progress", Json);
        frames.Add(new GoldenFrame("progress-start", ProjectProgress(progress0!)));

        for (var i = 0; i < 30; i++)
        {
            var item = bank.AllItems[i];
            var value = ((i * 3) % 5) + 1;
            var save = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{item.Id}",
                new PupilAnswerRequest(value));
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);
            var answerBody = await save.Content.ReadFromJsonAsync<JsonObject>(Json);
            frames.Add(new GoldenFrame($"answer-{i}", ProjectAnswer(answerBody!)));

            var progress = await client.GetFromJsonAsync<JsonObject>("api/pupil/progress", Json);
            frames.Add(new GoldenFrame($"progress-after-{i}", ProjectProgress(progress!)));
        }

        var chips = await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            ["tekenen", "dieren"],
            ["voor-de-klas"],
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, chips.StatusCode);
        var chipsBody = await chips.Content.ReadFromJsonAsync<JsonObject>(Json);
        frames.Add(new GoldenFrame("chips", ProjectChips(chipsBody!)));

        var progressIsland = await client.GetFromJsonAsync<JsonObject>("api/pupil/progress", Json);
        frames.Add(new GoldenFrame("progress-after-chips", ProjectProgress(progressIsland!)));

        for (var i = 30; i < 60; i++)
        {
            var item = bank.AllItems[i];
            var value = ((i * 2) % 5) + 1;
            var save = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{item.Id}",
                new PupilAnswerRequest(value));
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);
            var answerBody = await save.Content.ReadFromJsonAsync<JsonObject>(Json);
            frames.Add(new GoldenFrame($"answer-{i}", ProjectAnswer(answerBody!)));

            var progress = await client.GetFromJsonAsync<JsonObject>("api/pupil/progress", Json);
            frames.Add(new GoldenFrame($"progress-after-{i}", ProjectProgress(progress!)));
        }

        return new GoldenFlowSnapshot(frames);
    }

    /// <summary>
    /// Baseline fields only — appended 03a fields (questionSet/nextStep/nextPuzzleKey) are ignored
    /// so VO LegacyVo and Groep78 stay identical to the pre-refactor snapshot.
    /// </summary>
    private static JsonObject ProjectProgress(JsonObject raw)
    {
        var answers = raw["answers"] as JsonObject ?? new JsonObject();
        var ordered = new JsonObject();
        foreach (var key in answers.Select(p => p.Key).OrderBy(k => k, StringComparer.Ordinal))
        {
            ordered[key] = answers[key] is null ? null : JsonNode.Parse(answers[key]!.ToJsonString());
        }

        return new JsonObject
        {
            ["status"] = raw["status"]?.DeepClone(),
            ["currentIndex"] = raw["currentIndex"]?.DeepClone(),
            ["totalItems"] = raw["totalItems"]?.DeepClone(),
            ["answeredCount"] = raw["answeredCount"]?.DeepClone(),
            ["platesShed"] = raw["platesShed"]?.DeepClone(),
            ["newShell"] = raw["newShell"]?.DeepClone(),
            ["currentItemId"] = raw["currentItemId"]?.DeepClone(),
            ["currentWorldKey"] = raw["currentWorldKey"]?.DeepClone(),
            ["answers"] = ordered,
            ["windowOpen"] = raw["windowOpen"]?.DeepClone(),
            ["completed"] = raw["completed"]?.DeepClone(),
            ["needsIsland"] = raw["needsIsland"]?.DeepClone(),
            ["islandDone"] = raw["islandDone"]?.DeepClone(),
            ["likes"] = raw["likes"]?.DeepClone(),
            ["dislikes"] = raw["dislikes"]?.DeepClone(),
            ["likeOtherWord"] = raw["likeOtherWord"]?.DeepClone(),
            ["dislikeOtherWord"] = raw["dislikeOtherWord"]?.DeepClone(),
        };
    }

    private static JsonObject ProjectAnswer(JsonObject raw) => new()
    {
        ["currentIndex"] = raw["currentIndex"]?.DeepClone(),
        ["answeredCount"] = raw["answeredCount"]?.DeepClone(),
        ["platesShed"] = raw["platesShed"]?.DeepClone(),
        ["completed"] = raw["completed"]?.DeepClone(),
        ["nextItemId"] = raw["nextItemId"]?.DeepClone(),
        ["nextWorldKey"] = raw["nextWorldKey"]?.DeepClone(),
        ["needsIsland"] = raw["needsIsland"]?.DeepClone(),
        ["resultPending"] = raw["resultPending"]?.DeepClone(),
    };

    private static JsonObject ProjectChips(JsonObject raw) => new()
    {
        ["ok"] = raw["ok"]?.DeepClone(),
        ["currentIndex"] = raw["currentIndex"]?.DeepClone(),
        ["nextItemId"] = raw["nextItemId"]?.DeepClone(),
        ["nextWorldKey"] = raw["nextWorldKey"]?.DeepClone(),
    };

    private static string SerializeBaselineProjection(GoldenFlowSnapshot snapshot)
    {
        var root = new JsonObject
        {
            ["frames"] = new JsonArray(snapshot.Frames.Select(f => new JsonObject
            {
                ["name"] = f.Name,
                ["body"] = f.Body
            }).ToArray())
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
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

        throw new InvalidOperationException("Jobsy.sln not found from " + AppContext.BaseDirectory);
    }

    private async Task EnableSchoolsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            row = new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = true
            };
            db.PlatformFeatureSettings.Add(row);
        }
        else
        {
            row.SchoolsEnabled = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task<SeedInfo> SeedOpenClassAsync(SchoolLevel level, int year, PupilQuestionSet set)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();

        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Golden School " + Guid.NewGuid().ToString("N")[..6],
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"voorbeeldcollege.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _factory.AdminId
        };
        db.Schools.Add(school);

        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = set == PupilQuestionSet.Groep78 ? "8A" : "2B",
            Level = level,
            Year = year,
            QuestionSet = set,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 3,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _factory.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();

        var generated = await codes.GenerateAsync(3, cls);
        var plain = codes.Unprotect(generated[0].CodeProtected)!;
        return new SeedInfo(school.Id, cls.Id, generated[0].Id, plain);
    }

    private sealed record SeedInfo(Guid SchoolId, Guid ClassId, Guid CodeId, string PlainCode);

    private sealed record GoldenFlowSnapshot(IReadOnlyList<GoldenFrame> Frames);

    private sealed record GoldenFrame(string Name, JsonObject Body);
}
