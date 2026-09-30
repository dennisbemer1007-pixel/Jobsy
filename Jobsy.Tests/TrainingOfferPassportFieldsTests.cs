using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class TrainingOfferPassportFieldsTests
{
    [Fact]
    public async Task Existing_offer_defaults_show_in_passport_false()
    {
        await using var db = CreateDb();
        var provider = new TrainingProvider
        {
            Id = Guid.NewGuid(),
            Name = "TestProvider",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://example.com/",
            FieldsCsv = "vaardigheden",
            Region = "Den Haag",
            IsActive = true
        };
        db.TrainingProviders.Add(provider);
        db.TrainingOffers.Add(new TrainingOffer
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Title = "Legacy",
            FieldsCsv = "vaardigheden",
            KeysCsv = "plannen",
            ExternalPath = "/cursus/legacy",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var row = await db.TrainingOffers.SingleAsync();
        Assert.False(row.ShowInPassport);
        Assert.False(row.IsFree);
        Assert.False(row.IsPartner);
        Assert.Null(row.AffiliateCode);
    }

    [Fact]
    public async Task Upsert_rejects_partner_without_affiliate_and_both_flags()
    {
        await using var db = CreateDb();
        var providerId = Guid.NewGuid();
        db.TrainingProviders.Add(new TrainingProvider
        {
            Id = providerId,
            Name = "P",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://example.com/",
            FieldsCsv = "vaardigheden",
            Region = "DH",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = new TrainingUpskillService(db, Config(), new FakeHost(Environments.Development));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpsertOfferAsync(new TrainingOfferUpsertRequest(
            null, providerId, "Partner course", "vaardigheden", "plannen", "/cursus/x", true, 1,
            IsPartner: true, AffiliateCode: null, ShowInPassport: true)));

        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpsertOfferAsync(new TrainingOfferUpsertRequest(
            null, providerId, "Both", "vaardigheden", "plannen", "/cursus/y", true, 1,
            IsFree: true, IsPartner: true, AffiliateCode: "A", ShowInPassport: true)));
    }

    [Fact]
    public async Task RecommendPassport_empty_without_curated_free()
    {
        await using var db = CreateDb();
        var sut = new TrainingUpskillService(db, Config(), new FakeHost(Environments.Production));
        var cards = await sut.RecommendPassportAsync(Guid.NewGuid(), "plannen", null);
        Assert.Empty(cards);
    }

    [Fact]
    public async Task Track_appends_affiliate_code()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "c@jobsy.local",
            FullName = "C",
            Role = UserRole.Candidate,
            IsActive = true
        });
        var providerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        db.TrainingProviders.Add(new TrainingProvider
        {
            Id = providerId,
            Name = "ZorgStart",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://zorgstart.example/",
            FieldsCsv = "vaardigheden",
            Region = "Den Haag",
            IsActive = true
        });
        db.TrainingOffers.Add(new TrainingOffer
        {
            Id = offerId,
            ProviderId = providerId,
            Title = "Plannen",
            FieldsCsv = "vaardigheden",
            KeysCsv = "plannen",
            ExternalPath = "/cursus/plannen",
            IsActive = true,
            IsPartner = true,
            AffiliateCode = "AFF99",
            ShowInPassport = true
        });
        await db.SaveChangesAsync();

        var sut = new TrainingUpskillService(db, Config(), new FakeHost(Environments.Development));
        var tracked = await sut.TrackAsync(userId, offerId, TrainingTracking.CampaignCompetence);
        Assert.Contains("aff=AFF99", tracked.Url, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_has_attribute_and_false_defaults()
    {
        var path = Path.Combine(FindRepoRoot(),
            "Jobsy.Infrastructure/Data/Migrations/20260929140000_AddTrainingOfferPassportFields.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Migration(\"20260929140000_AddTrainingOfferPassportFields\")", text, StringComparison.Ordinal);
        Assert.Contains("ShowInPassport", text, StringComparison.Ordinal);
        Assert.Contains("defaultValue: false", text, StringComparison.Ordinal);
        Assert.DoesNotContain("InsertData", text, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repo root not found.");
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static IConfiguration Config()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Training:TrackingSecret"] = "unit-test-secret",
            ["Training:SeedDemoProviders"] = "false"
        }).Build();

    private sealed class FakeHost(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
