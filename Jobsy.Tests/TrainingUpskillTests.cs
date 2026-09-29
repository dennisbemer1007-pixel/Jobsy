using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class TrainingUpskillTests
{
    [Fact]
    public void Tracking_never_puts_email_or_guid_in_the_query()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var clickId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var hash = TrainingTracking.CandidateHash(userId, "secret");
        Assert.Equal(16, hash.Length);
        Assert.DoesNotContain(userId.ToString("N"), hash, StringComparison.OrdinalIgnoreCase);

        var url = TrainingTracking.AppendParameters(
            "https://www.loi.nl/zorg",
            hash,
            clickId,
            TrainingTracking.CampaignFit,
            "affiliate");
        Assert.Contains("ref=lobsy", url, StringComparison.Ordinal);
        Assert.Contains("candidate_id=" + hash, url, StringComparison.Ordinal);
        Assert.Contains("campaign=functie_fit", url, StringComparison.Ordinal);
        Assert.Contains("utm_source=lobsy", url, StringComparison.Ordinal);
        Assert.DoesNotContain("@", url, StringComparison.Ordinal);
        Assert.DoesNotContain(userId.ToString("D"), url, StringComparison.OrdinalIgnoreCase);
        Assert.True(TrainingTracking.LooksSafeOutbound(url));
        Assert.False(TrainingTracking.LooksSafeOutbound("https://evil.example/?e=ada@jobsy.local"));
    }

    [Fact]
    public void Field_catalog_detects_shortage_roles()
    {
        Assert.Contains(TrainingFieldCatalog.Zorg, TrainingFieldCatalog.Detect(["Verpleegkundige"]));
        Assert.Contains(TrainingFieldCatalog.Techniek, TrainingFieldCatalog.Detect(["Monteur installatie"]));
        Assert.Contains(TrainingFieldCatalog.Logistiek, TrainingFieldCatalog.Detect(["Heftruck chauffeur"]));
        Assert.Contains(TrainingFieldCatalog.Vaardigheden, TrainingFieldCatalog.Detect(["samenwerken communicatie teamoverleg"]));
        Assert.DoesNotContain(TrainingFieldCatalog.Vaardigheden, TrainingFieldCatalog.Detect(["Verpleegkundige"]));
    }

    [Fact]
    public void Regional_partner_outscores_national_fallback_on_a_zorg_gap()
    {
        var fields = new[] { TrainingFieldCatalog.Zorg };
        var regional = TrainingMatchRules.Score(
            TrainingProviderKind.RegionalPartner, fields, ["zorg"], fields, "verpleegkundige");
        var national = TrainingMatchRules.Score(
            TrainingProviderKind.NationalAffiliate, fields, ["zorg"], fields, "verpleegkundige");
        var nationalNoKey = TrainingMatchRules.Score(
            TrainingProviderKind.NationalAffiliate, fields, ["loi"], fields, "verpleegkundige");
        Assert.True(regional > national);
        Assert.Equal(0, nationalNoKey);
    }

    [Fact]
    public async Task Service_recommends_haaglanden_first_tracks_hashed_url_and_exports_csv()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "ada@jobsy.local",
            FullName = "Ada",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = new TrainingUpskillService(db, Config(), new FakeHostEnvironment(Environments.Development));
        var cards = await sut.RecommendAsync(userId, "Verpleegkundige", ["zorg"], TrainingTracking.CampaignFit);
        Assert.NotEmpty(cards);
        Assert.Equal("Zorgcollege Haaglanden", cards[0].ProviderName);
        Assert.Equal(TrainingCopy.Cta, cards[0].CtaLabel);
        Assert.Equal(TrainingCopy.GapAdvice, cards[0].Advice);

        var tracked = await sut.TrackAsync(userId, cards[0].OfferId, TrainingTracking.CampaignFit);
        Assert.True(TrainingTracking.LooksSafeOutbound(tracked.Url));
        Assert.DoesNotContain("ada@jobsy.local", tracked.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(userId.ToString("D"), tracked.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ref=lobsy", tracked.Url, StringComparison.Ordinal);
        Assert.True(TrainingDeepLinkRules.IsCourseDeepLink(tracked.Url));
        Assert.DoesNotContain("https://www.rocmondriaan.nl/?ref=", tracked.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/opleidingen/", tracked.Url, StringComparison.OrdinalIgnoreCase);

        var lead = await sut.RecordConversionAsync(new(tracked.ClickId, null, null, TrainingConversionKind.Lead));
        Assert.Equal("Lead", lead.Kind);
        var started = await sut.RecordConversionAsync(new(null, null, "ada@jobsy.local", TrainingConversionKind.Started));
        Assert.Equal("Started", started.Kind);

        var csv = await sut.ExportCsvAsync(DateTime.UtcNow.Year, DateTime.UtcNow.Month, null);
        Assert.Contains("Zorgcollege Haaglanden", csv, StringComparison.Ordinal);
        Assert.Contains(tracked.CandidateHash, csv, StringComparison.Ordinal);
        Assert.DoesNotContain("ada@jobsy.local", csv, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lobsy-export", csv, StringComparison.Ordinal);

        var skills = await sut.RecommendAsync(
            userId,
            CompetencyTrainingCatalog.SearchBlob(CompetencyTestCatalog.Samenwerken),
            null,
            TrainingTracking.CampaignCompetence);
        Assert.NotEmpty(skills);
        Assert.Equal("Praktijkacademie Haaglanden", skills[0].ProviderName);
        Assert.Equal(TrainingCopy.SkillCta, skills[0].CtaLabel);
        Assert.Equal(TrainingCopy.SkillAdvice, skills[0].Advice);
        Assert.Contains("Samenwerken", skills[0].Title, StringComparison.OrdinalIgnoreCase);

        await sut.ForgetUserAsync(userId);
        var click = await db.TrainingClicks.SingleAsync();
        Assert.Null(click.UserId);
        Assert.False(string.IsNullOrWhiteSpace(click.CandidateHash));
    }

    [Fact]
    public async Task EnsureDefaults_in_production_without_seed_switch_adds_nothing()
    {
        await using var db = CreateDb();
        var sut = new TrainingUpskillService(
            db,
            Config(seedDemoProviders: false),
            new FakeHostEnvironment(Environments.Production));

        await sut.EnsureDefaultsAsync();

        Assert.Empty(await db.TrainingProviders.ToListAsync());
        Assert.Empty(await db.TrainingOffers.ToListAsync());
    }

    [Fact]
    public async Task EnsureDefaults_in_development_seeds_demo_providers()
    {
        await using var db = CreateDb();
        var sut = new TrainingUpskillService(db, Config(), new FakeHostEnvironment(Environments.Development));

        await sut.EnsureDefaultsAsync();

        Assert.Equal(6, await db.TrainingProviders.CountAsync());
        Assert.All(TrainingDemoSeed.DemoProviderIds, id => Assert.True(db.TrainingProviders.Any(p => p.Id == id)));
    }

    [Fact]
    public async Task EnsureDefaults_with_seed_switch_on_seeds_in_production()
    {
        await using var db = CreateDb();
        var sut = new TrainingUpskillService(
            db,
            Config(seedDemoProviders: true),
            new FakeHostEnvironment(Environments.Production));

        await sut.EnsureDefaultsAsync();

        Assert.Equal(6, await db.TrainingProviders.CountAsync());
    }

    [Fact]
    public async Task EnsureDefaults_in_test_environment_seeds_demo_providers()
    {
        await using var db = CreateDb();
        var sut = new TrainingUpskillService(
            db,
            Config(seedDemoProviders: false),
            new FakeHostEnvironment("Test"));

        await sut.EnsureDefaultsAsync();

        Assert.Equal(6, await db.TrainingProviders.CountAsync());
    }

    [Fact]
    public async Task RecommendAsync_with_only_inactive_providers_returns_empty()
    {
        await using var db = CreateDb();
        foreach (var seed in TrainingDemoSeed.Seeds())
        {
            seed.IsActive = false;
            foreach (var offer in seed.Offers)
            {
                offer.IsActive = true;
            }

            db.TrainingProviders.Add(seed);
        }

        await db.SaveChangesAsync();

        var sut = new TrainingUpskillService(
            db,
            Config(seedDemoProviders: false),
            new FakeHostEnvironment(Environments.Production));
        var cards = await sut.RecommendAsync(Guid.NewGuid(), "Verpleegkundige", ["zorg"], TrainingTracking.CampaignFit);
        Assert.Empty(cards);
    }

    [Fact]
    public void Deactivate_demo_providers_migration_updates_the_six_seed_ids()
    {
        Assert.Equal(6, TrainingDemoSeed.DemoProviderIds.Length);
        var migration = File.ReadAllText(Path.Combine(
            RepoRoot.Find(),
            "Jobsy.Infrastructure/Data/Migrations/20260929130000_DeactivateDemoTrainingProviders.cs"));
        Assert.Contains("UPDATE \"TrainingProviders\"", migration, StringComparison.Ordinal);
        Assert.Contains("SET \"IsActive\" = false", migration, StringComparison.Ordinal);
        Assert.Contains("SET \"IsActive\" = true", migration, StringComparison.Ordinal);
        foreach (var id in TrainingDemoSeed.DemoProviderIds)
        {
            Assert.Contains(id.ToString("D"), migration, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("DELETE FROM", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DropTable", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void TrainingOffersBlock_with_empty_list_renders_nothing()
    {
        var block = File.ReadAllText(Path.Combine(
            RepoRoot.Find(),
            "Jobsy.Web/Components/Candidate/TrainingOffersBlock.razor"));
        Assert.Contains("_offers.Count == 0 && string.IsNullOrWhiteSpace(_message)", block, StringComparison.Ordinal);
        Assert.Contains("Offers is not null", block, StringComparison.Ordinal);
        // Early return before the section markup when there is nothing to show.
        var earlyReturnIdx = block.IndexOf("_offers.Count == 0", StringComparison.Ordinal);
        var sectionIdx = block.IndexOf("<section", StringComparison.Ordinal);
        Assert.True(earlyReturnIdx >= 0 && sectionIdx > earlyReturnIdx);
        Assert.Contains("return;", block[earlyReturnIdx..sectionIdx], StringComparison.Ordinal);
    }

    [Fact]
    public void Functie_fit_and_compass_surfaces_include_the_cta()
    {
        var snapshot = RoleFitCheckBuilder.Build(
            "Verpleegkundige",
            new CompetencyScores(88, 70, 72, 40),
            new RiasecScores(20, 30, 25, 95, 40, 35),
            fromDeepAnalysis: false);
        Assert.Contains(snapshot.ActionSteps, s => s == TrainingCopy.GapAdvice);

        var compass = CareerCompassBuilder.Build(new RiasecScores(20, 30, 25, 95, 40, 35), fromDeepAnalysis: true);
        Assert.Contains(TrainingCopy.GapAdvice, compass.PracticalNotes);

        var panel = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Candidate/RoleFitCheckPanel.razor"));
        Assert.Contains("TrainingOffersBlock", panel, StringComparison.Ordinal);
        var compassPanel = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Candidate/CareerCompassPanel.razor"));
        Assert.Contains("TrainingOffersBlock", compassPanel, StringComparison.Ordinal);
        var competencyPanel = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Candidate/CompetencyScorePanel.razor"));
        Assert.Contains("TrainingOffersBlock", competencyPanel, StringComparison.Ordinal);
        Assert.Contains("CampaignCompetence", competencyPanel, StringComparison.Ordinal);
        var admin = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Pages/Admin/TrainingAdmin.razor"));
        Assert.Contains("/admin/training", admin, StringComparison.Ordinal);
        Assert.Equal("Passende cursus", Jobsy.Web.Localization.UiStrings.Get("Fit.TrainingTitle", "nl"));
        Assert.Contains(
            TrainingCopy.GapAdvice,
            Jobsy.Web.Localization.UiStrings.Get("Fit.TrainingLead", "nl"),
            StringComparison.Ordinal);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static IConfiguration Config(bool? seedDemoProviders = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Training:TrackingSecret"] = "unit-test-secret"
        };
        if (seedDemoProviders is bool flag)
        {
            values["Training:SeedDemoProviders"] = flag ? "true" : "false";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
