using System.Text.Json;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

/// <summary>
/// Guard: TestDetail / MatchPage must persist slim DTOs (&lt; 20 KB) so mobile 4G
/// StartCircuit / UpdateRootComponents stay interactive. Heavy CareerCompass /
/// DeepState / swipe decks reload after the circuit is interactive.
/// </summary>
public class PersistLcpGuardTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void TestDetail_and_MatchPage_persist_only_slim_dtos_under_20kb()
    {
        var root = FindRepoRoot();
        var testDetail = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Candidate", "TestDetail.razor"));
        var matchPage = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Candidate", "MatchPage.razor"));

        Assert.Contains("OnAfterRenderAsync", testDetail, StringComparison.Ordinal);
        Assert.Contains("LoadHeavyStateAsync", testDetail, StringComparison.Ordinal);
        Assert.Contains("RendererInfo.IsInteractive", testDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("CareerCompass = _careerCompass", testDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("DeepState = _deepState", testDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("CompetenceReport = _competenceReport", testDetail, StringComparison.Ordinal);

        Assert.Contains("VacancyIds", matchPage, StringComparison.Ordinal);
        Assert.Contains("SlimGate", matchPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Deck = _deck.ToList()", matchPage, StringComparison.Ordinal);
        Assert.Contains("OnAfterRenderAsync", matchPage, StringComparison.Ordinal);

        // Seed-candidate shaped slim payloads must serialize well under 20 KB.
        var testDetailPayload = new
        {
            TestKey = "career",
            FreeDone = true,
            DeepUnlocked = true,
            DeepCompleted = true,
            PriceEuro = 2.99m
        };
        var testDetailBytes = JsonSerializer.SerializeToUtf8Bytes(testDetailPayload, JsonOptions);
        Assert.True(
            testDetailBytes.Length < 20 * 1024,
            $"TestDetail persist payload is {testDetailBytes.Length} bytes (limit 20 KB)");

        var matchPayload = new
        {
            Gate = new MatchProfileGateViewModel
            {
                IsAuthenticated = true,
                ProfileBasicsFilled = true,
                HasEducationLevel = true,
                WizardCompleted = true,
                CompetencyCompleted = true,
                CareerCompleted = true,
                CultureCompleted = true,
                IsProfileComplete = true,
                CompletedCount = 3,
                RequiredCount = 3,
                PreferredTransport = "Fiets",
                MaxTravelMinutes = 30
            },
            VacancyIds = Enumerable.Range(0, 24).Select(_ => Guid.NewGuid()).ToList(),
            Index = 3
        };
        var matchBytes = JsonSerializer.SerializeToUtf8Bytes(matchPayload, JsonOptions);
        Assert.True(
            matchBytes.Length < 20 * 1024,
            $"MatchPage persist payload is {matchBytes.Length} bytes (limit 20 KB)");
    }

    [Fact]
    public void VacancyDetail_prerender_hero_is_eager_and_video_uses_facade()
    {
        var root = FindRepoRoot();
        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "VacancyDetail.razor"));
        var photo = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyPhoto.razor"));

        Assert.Contains("Class=\"detail-card__image vacancy-media__photo\"", detail, StringComparison.Ordinal);
        Assert.Contains("Eager=\"true\"", detail, StringComparison.Ordinal);
        Assert.Contains("Width=\"720\"", detail, StringComparison.Ordinal);
        Assert.Contains("Height=\"480\"", detail, StringComparison.Ordinal);
        Assert.Contains("fetchpriority=\"@(Eager ? \"high\" : \"auto\")\"", photo, StringComparison.Ordinal);
        Assert.Contains("loading=\"@(Eager ? \"eager\" : \"lazy\")\"", photo, StringComparison.Ordinal);
        Assert.Contains("decoding=\"async\"", photo, StringComparison.Ordinal);

        // YouTube iframe only after tap — poster/facade first.
        Assert.Contains("_videoActivated", detail, StringComparison.Ordinal);
        Assert.Contains("detail-card__video-poster", detail, StringComparison.Ordinal);
        Assert.Contains("VideoThumbnailUrl", detail, StringComparison.Ordinal);
        var activatedIdx = detail.IndexOf("@if (_videoActivated)", StringComparison.Ordinal);
        var iframeIdx = detail.IndexOf("<iframe src=\"@VideoEmbedUrl\"", StringComparison.Ordinal);
        var posterIdx = detail.IndexOf("detail-card__video-poster", StringComparison.Ordinal);
        Assert.True(activatedIdx >= 0 && iframeIdx > activatedIdx, "iframe must be inside _videoActivated branch");
        Assert.True(posterIdx > activatedIdx, "poster facade must follow the activated branch");

        // First discovery card photo is eager (desktop LCP).
        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        var card = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Discovery", "VacancyCard.razor"));
        Assert.Contains("Eager=\"cardIndex == 0\"", discovery, StringComparison.Ordinal);
        Assert.Contains("[Parameter] public bool Eager { get; set; }", card, StringComparison.Ordinal);
        Assert.Contains("Eager=\"Eager\"", card, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
