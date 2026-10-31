using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Careers;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests;

public class OccupationDayDagtekstenTests
{
    private const string CookId = "90f75f67-495d-49fa-ab57-2f320e251d7e";
    private const string ClusterGeofysicusId = "a92a3e24-93f0-42b3-9ebb-d2d7d8a72cc5";

    [Fact]
    public void Timeline_accepts_nine_blocks_with_evening_hours()
    {
        var blocks = KokBlocks();
        Assert.Equal(9, blocks.Count);
        Assert.True(OccupationDayTimeline.TryValidateDagteksten(blocks, out var reason), reason);
        Assert.True(OccupationDayTimeline.ShouldPreserveLabels(blocks));
        var relabeled = OccupationDayTimeline.AssignChronologicalLabels(blocks);
        Assert.Equal("08:00 Voorbereiding", relabeled[0].Label);
        Assert.Equal("20:00 Opruimen", relabeled[^1].Label);
    }

    [Fact]
    public void AssignChronologicalLabels_skips_standard_school_schedule_when_not_applicable()
    {
        var blocks = KokBlocks();
        var repaired = OccupationDayDraftRepair.Apply(
            OccupationDayBlocks.WithDerived(new OccupationDayDraft(
                "kok",
                "",
                "",
                "",
                "",
                ["Je werkt met taken uit dit beroep.", "De volgorde kan per dag verschillen."],
                "De volgorde en taken kunnen per werkgever verschillen. Dat is normaal in dit beroep.",
                blocks)),
            OccupationDayFacts.For(CookId)!);
        Assert.Equal("10:30 Koken voor de lunch", repaired.Blocks![2].Label);
    }

    [Fact]
    public async Task Dagteksten_importer_upserts_kok_and_cluster_rows()
    {
        var path = Path.Combine(RepoRoot.Find(), "Jobsy.Tests/Fixtures/dagteksten-sample.json");
        await using var db = NewDb();
        var importer = new OccupationDayDagtekstenImporter(db, NullLogger<OccupationDayDagtekstenImporter>.Instance);
        await using var stream = File.OpenRead(path);
        var document = OccupationDayDagtekstenImporter.ParseDocument(stream);
        Assert.NotNull(document);

        var first = await importer.ImportAsync(document!);
        Assert.True(first.Mapped > 0);
        Assert.True(first.Inserted > 0);
        Assert.Equal(0, first.Rejected);

        var stored = await db.OccupationDayInLives.AsNoTracking().SingleAsync(row => row.EscoId == CookId);
        var blocks = OccupationDayBlocks.Parse(stored.BlocksJson);
        Assert.Equal(9, blocks.Count);
        Assert.Equal("1", blocks[0].Key);
        Assert.StartsWith("08:00", blocks[0].Label, StringComparison.Ordinal);
        Assert.StartsWith("20:00", blocks[^1].Label, StringComparison.Ordinal);
        Assert.Equal(OccupationDayDagtekstenImport.SourceModel, stored.SourceModel);

        var cluster = await db.OccupationDayInLives.AsNoTracking().SingleAsync(row => row.EscoId == ClusterGeofysicusId);
        Assert.True(OccupationDayBlocks.UsesNumberedTimeline(OccupationDayBlocks.Parse(cluster.BlocksJson)));

        stream.Position = 0;
        document = OccupationDayDagtekstenImporter.ParseDocument(stream);
        var second = await importer.ImportAsync(document!);
        Assert.Equal(first.Mapped, second.Mapped);
        Assert.Equal(0, second.Inserted);
        Assert.True(second.Unchanged >= first.Mapped - second.Updated);
    }

    [Fact]
    public void Admin_screen_has_dagteksten_upload()
    {
        var page = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Pages/Admin/OccupationDayAdmin.razor"));
        var controller = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Api/Controllers/OccupationDayInLifeAdminController.cs"));
        Assert.Contains("ImportDagtekstenAsync", page, StringComparison.Ordinal);
        Assert.Contains("import-dagteksten", controller, StringComparison.Ordinal);
        Assert.Equal("Importeer dagteksten.json", UiStrings.Get("Admin.Day.ImportDagteksten", "nl"));
    }

    [Fact]
    public void Candidate_heading_uses_catalog_title_for_cluster_and_kok()
    {
        var page = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Pages/Candidate/OccupationDayPage.razor"));
        Assert.Contains("Culture.Format(\"Day.Title\", Job.Nl)", page, StringComparison.Ordinal);
        Assert.DoesNotContain("_day.TitleNl", page, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1440, 900, "cluster-geofysicus-1440.png")]
    [InlineData(390, 844, "cluster-geofysicus-390.png")]
    [InlineData(1280, 900, "kok-day-1280.png")]
    public async Task Screen_13_renders_dagteksten_timelines(int width, int height, string fileName)
    {
        var escoId = fileName.StartsWith("kok", StringComparison.Ordinal) ? CookId : ClusterGeofysicusId;
        var blocks = fileName.StartsWith("kok", StringComparison.Ordinal)
            ? KokBlocks()
            : SampleClusterBlocks();

        await using var ctx = new OccupationDayRenderContext();
        var cut = ctx.RenderDay(escoId, blocks);
        if (fileName.StartsWith("kok", StringComparison.Ordinal))
        {
            Assert.Equal(9, cut.FindAll(".day-life__block").Count);
            Assert.Contains("20:00", cut.Markup, StringComparison.Ordinal);
        }
        else
        {
            Assert.True(cut.FindAll(".day-life__block").Count is >= 7 and <= 10);
            Assert.Contains(OccupationCatalog.Shared.Get(escoId)!.Nl, cut.Markup, StringComparison.Ordinal);
        }

        var outDir = Path.Combine(RepoRoot.Find(), "Jobsy.Tests/Artifacts/occupation-day-dagteksten");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, fileName);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            HasTouch = width < 768,
            IsMobile = width < 768
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(HtmlShell(cut.Markup));
        await page.ScreenshotAsync(new() { Path = outPath, FullPage = true });
        Assert.True(File.Exists(outPath));
    }

    private static List<OccupationDayBlock> KokBlocks()
        =>
        [
            new("1", "08:00 Voorbereiding", "Je begint met mise en place."),
            new("2", "09:00 Ingrediënten verzamelen", "Je haalt producten uit de koeling."),
            new("3", "10:30 Koken voor de lunch", "Je bereidt gerechten voor de lunch."),
            new("4", "12:00 Lunchdrukte", "Je serveert en werkt snel door."),
            new("5", "14:00 Pauze", "Je eet iets en drinkt water."),
            new("6", "14:30 Voorbereiding diner", "Je snijdt groenten voor het diner."),
            new("7", "16:00 Koken voor het diner", "Je kookt de hoofdgerechten."),
            new("8", "18:00 Dinerdrukte", "Je werkt met het team in de pass."),
            new("9", "20:00 Opruimen", "Je ruimt je station op.")
        ];

    private static List<OccupationDayBlock> SampleClusterBlocks()
        =>
        [
            new("1", "08:00 Start", "Je bekijkt de metingen van gisteren."),
            new("2", "09:30 Analyse", "Je vergelijkt data met eerdere dagen."),
            new("3", "11:00 Overleg", "Je bespreekt resultaten met het team."),
            new("4", "12:30 Pauze", "Je pauzeert kort."),
            new("5", "13:30 Veldwerk", "Je bekijkt instrumenten op locatie."),
            new("6", "15:00 Rapport", "Je schrijft bevindingen op."),
            new("7", "16:30 Afronden", "Je deelt de resultaten.")
        ];

    private static string HtmlShell(string body)
    {
        var cssPath = Path.Combine(RepoRoot.Find(), "Jobsy.Web/wwwroot/css/features/occupation-day.css");
        var css = File.ReadAllText(cssPath);
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\"/><style>"
            + ":root{--coral:#e86a4a;--coral-deep:#b94d34;--coral-soft:#fde8e2;--brand:#1f3d7a;--text:#1a1a1a;--muted:#5a6472;--border:#d8dee8;--surface:#fff;--r-md:12px;--r-lg:16px;--radius-pill:999px;}"
            + css
            + "</style></head><body style=\"margin:16px;background:#f6f8fb;font-family:system-ui,sans-serif\">"
            + body
            + "</body></html>";
    }

    private static JobsyDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class OccupationDayRenderContext : BunitContext
    {
        private readonly HttpClient _http = new(new DayApi()) { BaseAddress = new Uri("http://localhost/") };

        public OccupationDayRenderContext()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth(["Candidate"]));
            Services.AddSingleton(sp => new CultureState(
                sp.GetRequiredService<IJSRuntime>(),
                sp,
                sp.GetRequiredService<AuthenticationStateProvider>()));
            Services.AddSingleton(new JobsyApiClient(_http));
        }

        public new async ValueTask DisposeAsync()
        {
            _http.Dispose();
            await base.DisposeAsync();
        }

        public IRenderedComponent<OccupationDayPage> RenderDay(string escoId, IReadOnlyList<OccupationDayBlock> blocks)
        {
            DayApi.Blocks = blocks;
            DayApi.EscoId = escoId;
            return Render<OccupationDayPage>(p => p.Add(x => x.EscoId, Guid.Parse(escoId)));
        }
    }

    private sealed class DayApi : HttpMessageHandler
    {
        public static string EscoId { get; set; } = "";
        public static IReadOnlyList<OccupationDayBlock> Blocks { get; set; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.Contains("occupation-day-in-life", StringComparison.Ordinal) == true)
            {
                var parts = OccupationDayBlocks.SplitIntoFour(Blocks.Select(b => b.Text).ToList());
                var payload = new
                {
                    enabled = true,
                    found = true,
                    escoId = EscoId,
                    titleNl = OccupationCatalog.Shared.Get(EscoId)?.Nl,
                    morning = parts[0],
                    midday = parts[1],
                    afternoon = parts[2],
                    closing = parts[3],
                    highlights = Array.Empty<string>(),
                    variesNote = "De volgorde kan per werkgever verschillen.",
                    thinSource = false,
                    language = "nl",
                    blocks = Blocks,
                    tasks = Array.Empty<string>(),
                    skills = Array.Empty<string>()
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(payload)
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FakeAuth(string[] roles) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }
}
