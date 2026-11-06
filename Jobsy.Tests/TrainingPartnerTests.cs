using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class TrainingPartnerTests
{
    [Fact]
    public void FormatRelevanceReason_prefers_custom_copy_over_fallback()
    {
        var nl = CareerCourseMatcher.FormatRelevanceReason(
            english: false,
            reasonNl: "Dit is je volgende stap naar dierenarts.",
            reasonEn: null,
            fallbackNl: "x",
            fallbackEn: "y");
        Assert.Contains("dierenarts", nl, StringComparison.Ordinal);
        Assert.DoesNotContain("%", nl, StringComparison.Ordinal);
    }

    [Fact]
    public void CompetencySkillBands_never_use_percent_labels()
    {
        Assert.Equal("Training.CompetencyBand.GrowHere", CompetencySkillBands.BandKey(30));
        Assert.Equal("Training.CompetencyBand.Strong", CompetencySkillBands.BandKey(80));
    }

    [Fact]
    public async Task Demo_seed_creates_partner_offers_in_development()
    {
        await using var db = CreateDb();
        var sut = new TrainingPartnerService(
            db,
            new TrainingPartnerFeedImporter(db),
            Config(),
            new FakeHostEnvironment(Environments.Development));

        await sut.EnsureDemoPartnersAsync();
        var count = await db.TrainingOffers.Include(o => o.Provider).CountAsync(o => o.Provider.IsDemoPartner);
        Assert.True(count >= 8);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static IConfiguration Config()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    private sealed class FakeHostEnvironment(string env) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = env;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public void Feature_flag_snapshot_exposes_training_partners()
    {
        var on = new Jobsy.Core.Features.FeatureFlagSnapshot(false, true, TrainingPartnersEnabled: true);
        Assert.True(on.IsEnabled(Jobsy.Core.Features.PlatformFeature.TrainingPartners));
    }
}
