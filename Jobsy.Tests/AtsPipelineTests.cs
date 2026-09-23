using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class AtsPipelineTests
{
    [Fact]
    public void Validation_rejects_missing_location_and_error_titles()
    {
        Assert.False(AtsListingValidation.TryValidateForReview(
            "404 Not Found", "Acme", "Den Haag", new string('x', 80), out _));
        Assert.False(AtsListingValidation.TryValidateForReview(
            "Kassamedewerker", "Acme", null, new string('x', 80), out var reason));
        Assert.Contains("locatie", reason!, StringComparison.OrdinalIgnoreCase);
        Assert.True(AtsListingValidation.TryValidateForReview(
            "Kassamedewerker", "Acme", "Naaldwijk", new string('x', 80), out _));
        Assert.True(AtsListingValidation.IsDemoListing("Kassamedewerker (demo)", "https://x/demo-abc", null));
    }

    [Theory]
    [InlineData("https://www.randstad.nl/vacatures", "Kassamedewerker", true)]
    [InlineData("https://werkenbij.denhaag.nl/vacature/1", "Beleidsadviseur", false)]
    [InlineData("https://tuinder.example.nl/jobs/1", "Flex medewerker uitzendbureau", true)]
    [InlineData("https://www.indeed.com/viewjob", "Magazijn", true)]
    public void Blacklist_blocks_agencies_and_aggregators(string url, string title, bool blocked)
        => Assert.Equal(blocked, AtsBlacklistFilter.IsBlocked(url, title, "Acme"));

    [Fact]
    public void Domain_whitelist_allows_subdomains_only()
    {
        Assert.True(AtsBlacklistFilter.IsDomainAllowed(
            "https://werkenbij.denhaag.nl/vacature/1", "werkenbij.denhaag.nl"));
        Assert.False(AtsBlacklistFilter.IsDomainAllowed(
            "https://evil.example/vacature/1", "werkenbij.denhaag.nl"));
    }

    [Fact]
    public void Dedupe_hash_is_stable_and_normalized()
    {
        var a = AtsDedupeHash.Compute("Gemeente Den Haag", "Beleidsadviseur", "2595");
        var b = AtsDedupeHash.Compute("  gemeente  den  haag ", "BELEIDSADVISEUR", "2595");
        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);
    }

    [Fact]
    public void Completeness_score_rewards_filled_fields()
    {
        var low = AtsCompletenessScore.Compute("T", "C", null, "kort", null, null, null, null, null, null, null);
        var high = AtsCompletenessScore.Compute(
            "Kassamedewerker",
            "Supermarkt Westland",
            "Naaldwijk",
            new string('x', 100),
            "€14,50 per uur",
            14.5m,
            "16-24 uur",
            16, 24,
            "[\"retail\"]",
            "https://example.nl/vacature/1");
        Assert.True(high > low);
        Assert.InRange(high, 80, 100);
    }

    [Fact]
    public void ExtractDetailUrls_keeps_same_domain_vacancy_links()
    {
        const string html = """
            <html><body>
            <a href="/vacatures/kassa">Kassamedewerker</a>
            <a href="https://www.randstad.nl/x">Uitzend</a>
            <a href="/over-ons">Over ons</a>
            </body></html>
            """;
        var urls = AtsScrapeService.ExtractDetailUrls(
            html,
            "https://winkel.example.nl/vacatures",
            "winkel.example.nl");
        Assert.Contains(urls, u => u.Contains("/vacatures/kassa", StringComparison.Ordinal));
        Assert.DoesNotContain(urls, u => u.Contains("randstad", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Approve_creates_active_Ats_vacancy_for_match_feed()
    {
        await using var db = CreateDb();
        var sourceId = Guid.NewGuid();
        db.AtsScrapeSources.Add(new AtsScrapeSource
        {
            Id = sourceId,
            Name = "Tuinder Demo",
            Domain = "tuinder.example.nl",
            ListUrl = "https://tuinder.example.nl/vacatures",
            DefaultLatitude = 51.99,
            DefaultLongitude = 4.21,
            DefaultLocationLabel = "Westland",
            IsEnabled = true
        });
        var listingId = Guid.NewGuid();
        db.AtsScrapedListings.Add(new AtsScrapedListing
        {
            Id = listingId,
            SourceId = sourceId,
            DedupHash = AtsDedupeHash.Compute("Tuinder Demo", "Medewerker kas", "Westland"),
            SourceUrl = "https://tuinder.example.nl/vacatures/kas",
            CompanyName = "Tuinder Demo",
            Title = "Medewerker kas",
            LocationLabel = "Westland",
            Description = new string('d', 120),
            CompletenessScore = 70,
            Status = AtsListingStatus.PendingReview,
            ScrapedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(20),
            Latitude = 51.99,
            Longitude = 4.21
        });
        db.MinimumWageRates.Add(new MinimumWageRate
        {
            Id = Guid.NewGuid(),
            AgeYears = 21,
            HourlyRate = 14.06m,
            Label = "21+",
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await db.SaveChangesAsync();

        var moderation = new AtsVacancyModerationService(db, NullLogger<AtsVacancyModerationService>.Instance);
        var vacancy = await moderation.ApproveAsync(listingId);
        Assert.NotNull(vacancy);
        Assert.Equal(VacancyStatus.Active, vacancy!.Status);
        Assert.Equal(VacancySource.Ats, vacancy.CreatedVia);
        Assert.NotNull(vacancy.PublishedAtUtc);

        var listing = await db.AtsScrapedListings.SingleAsync(l => l.Id == listingId);
        Assert.Equal(AtsListingStatus.Approved, listing.Status);
        Assert.Equal(vacancy.Id, listing.LinkedVacancyId);

        // Match / banenkaart feed uses Active vacancies — ATS rows participate.
        var activeAts = await db.Vacancies.CountAsync(v =>
            v.Status == VacancyStatus.Active && v.CreatedVia == VacancySource.Ats);
        Assert.Equal(1, activeAts);

        var metrics = await new MetricsQueryService(db)
            .GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "week");
        Assert.Equal(1, metrics.First(m => m.Key == "active_vacancies_ats").Value);
        Assert.Equal(0, metrics.First(m => m.Key == "active_vacancies_regular").Value);

        var atsDrill = await new MetricsQueryService(db)
            .GetDrilldownAsync("active_vacancies_ats", includePlatformOnly: true, companyIds: null, period: "week");
        Assert.Single(atsDrill);
        Assert.Contains("ATS", atsDrill[0].Subtitle);
    }

    [Fact]
    public async Task Health_ttl_marks_listing_expired_and_archives_vacancy()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo",
            KvkNumber = "12345678",
            Address = "A",
            Location = new GeoPoint(52, 4)
        });
        var vacancyId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Oud",
            Description = "Oud",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            CreatedVia = VacancySource.Ats,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-40),
            Location = new GeoPoint(52, 4)
        });
        var sourceId = Guid.NewGuid();
        db.AtsScrapeSources.Add(new AtsScrapeSource
        {
            Id = sourceId,
            Name = "Demo",
            Domain = "demo.example.nl",
            ListUrl = "https://demo.example.nl/",
            DefaultLatitude = 52,
            DefaultLongitude = 4,
            IsEnabled = true
        });
        db.AtsScrapedListings.Add(new AtsScrapedListing
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            DedupHash = AtsDedupeHash.Compute("Demo", "Oud", "Den Haag"),
            SourceUrl = "https://demo.example.nl/oud",
            CompanyName = "Demo",
            Title = "Oud",
            Description = "Oud",
            Status = AtsListingStatus.Approved,
            ScrapedAtUtc = DateTime.UtcNow.AddDays(-40),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5),
            LinkedVacancyId = vacancyId
        });
        await db.SaveChangesAsync();

        var health = new AtsVacancyHealthService(
            db,
            new StubHttpClientFactory(),
            NullLogger<AtsVacancyHealthService>.Instance);
        var changed = await health.RunHealthPassAsync();
        Assert.True(changed >= 1);

        var listing = await db.AtsScrapedListings.SingleAsync();
        Assert.Equal(AtsListingStatus.Expired, listing.Status);
        var vacancy = await db.Vacancies.SingleAsync(v => v.Id == vacancyId);
        Assert.Equal(VacancyStatus.Archived, vacancy.Status);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("ats-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new HttpClientHandler());
    }
}
