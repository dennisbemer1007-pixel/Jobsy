using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent whitelist of direct local employer career sites (no agencies / aggregators).
/// Never seeds demo listings — overview shows only real scrapes.
/// </summary>
internal static class AtsScrapeSourceSeeder
{
    private const string SeedMarker = "ATS scrape source whitelist v2";
    private const string DemoPurgeMarker = "ATS demo listings purged v1";
    private const string UrlRepairMarker = "ATS scrape source URL repair v2 denhaag-westland";

    // Deterministic ids: a75c0000-0000-4000-8000-0000000000NN
    private static Guid SourceId(int n) =>
        Guid.Parse($"a75c0000-0000-4000-8000-{n:D12}");

    public static async Task SeedAsync(JobsyDbContext db, ILogger logger)
    {
        var sources = BuildSources();
        var added = 0;
        foreach (var source in sources)
        {
            if (await db.AtsScrapeSources.AnyAsync(s => s.Id == source.Id)
                || await db.AtsScrapeSources.AnyAsync(s => s.Domain == source.Domain))
            {
                continue;
            }

            db.AtsScrapeSources.Add(source);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("ATS scrape sources seeded: {Count} new whitelist entries.", added);
        }

        await RepairKnownBadSourcesAsync(db, logger);
        await PurgeDemoListingsAsync(db, logger);

        if (!await db.PlatformLogs.AnyAsync(l => l.Category == "Seed" && l.Message == SeedMarker))
        {
            db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Seed",
                Message = SeedMarker,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Fixes broken DNS hosts from earlier whitelist seeds (werkenbij.denhaag.nl,
    /// werkenbij.gemeentewestland.nl) and outdated Westland list paths.
    /// </summary>
    private static async Task RepairKnownBadSourcesAsync(JobsyDbContext db, ILogger logger)
    {
        var changed = false;

        // Repair known-bad Haga host from earlier whitelist seed (DNS failures).
        var hagaBroken = await db.AtsScrapeSources
            .FirstOrDefaultAsync(s => s.Domain == "werkenbijhagaziekenhuis.nl");
        if (hagaBroken is not null)
        {
            hagaBroken.Domain = "www.hagaziekenhuis.nl";
            hagaBroken.ListUrl = "https://www.hagaziekenhuis.nl/werken-bij-haga";
            hagaBroken.Name = "HagaZiekenhuis";
            changed = true;
            logger.LogInformation("ATS repaired Haga scrape source host to www.hagaziekenhuis.nl.");
        }

        var denHaag = await db.AtsScrapeSources
            .FirstOrDefaultAsync(s => s.Id == SourceId(1)
                                      || s.Domain == "werkenbij.denhaag.nl"
                                      || s.Domain == "werkenvoor.denhaag.nl");
        if (denHaag is not null)
        {
            if (!string.Equals(denHaag.Domain, "werkenvoor.denhaag.nl", StringComparison.OrdinalIgnoreCase)
                || !denHaag.ListUrl.Contains("werkenvoor.denhaag.nl", StringComparison.OrdinalIgnoreCase))
            {
                denHaag.Name = "Gemeente Den Haag";
                denHaag.Domain = "werkenvoor.denhaag.nl";
                denHaag.ListUrl = "https://werkenvoor.denhaag.nl/vacatures/";
                denHaag.DefaultLocationLabel = "Den Haag";
                denHaag.IsEnabled = true;
                changed = true;
                logger.LogInformation("ATS repaired Den Haag scrape source to werkenvoor.denhaag.nl.");
            }
        }

        // Broken DNS career subdomain — disable so it does not fail the scrape run.
        var westlandBroken = await db.AtsScrapeSources
            .FirstOrDefaultAsync(s => s.Id == SourceId(2)
                                      || s.Domain == "werkenbij.gemeentewestland.nl");
        if (westlandBroken is not null)
        {
            westlandBroken.Name = "Gemeente Westland (werkenbij — uitgeschakeld)";
            westlandBroken.Domain = "werkenbij.gemeentewestland.nl";
            westlandBroken.ListUrl = "https://werkenbij.gemeentewestland.nl/";
            westlandBroken.IsEnabled = false;
            changed = true;
            logger.LogInformation("ATS disabled broken Westland werkenbij host.");
        }

        var westland = await db.AtsScrapeSources
            .FirstOrDefaultAsync(s => s.Id == SourceId(3)
                                      || s.Domain == "www.gemeentewestland.nl");
        if (westland is not null)
        {
            const string correctList =
                "https://www.gemeentewestland.nl/bestuur-en-organisatie/over-de-organisatie/werken-voor-westland/vacatures";
            if (!string.Equals(westland.ListUrl, correctList, StringComparison.OrdinalIgnoreCase)
                || !westland.IsEnabled)
            {
                westland.Name = "Gemeente Westland";
                westland.Domain = "www.gemeentewestland.nl";
                westland.ListUrl = correctList;
                westland.DefaultLocationLabel = "Westland";
                westland.IsEnabled = true;
                changed = true;
                logger.LogInformation("ATS repaired Westland scrape list URL to werken-voor-westland/vacatures.");
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }

        if (!await db.PlatformLogs.AnyAsync(l => l.Category == "Seed" && l.Message == UrlRepairMarker))
        {
            db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Seed",
                Message = UrlRepairMarker,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>One-shot removal of previously seeded ATS demo rows from acceptatie/prod DBs.</summary>
    private static async Task PurgeDemoListingsAsync(JobsyDbContext db, ILogger logger)
    {
        var demos = await db.AtsScrapedListings
            .Where(l =>
                l.Title.Contains("(demo)")
                || l.SourceUrl.Contains("/demo-")
                || (l.TagsJson != null && l.TagsJson.Contains("\"demo\"")))
            .ToListAsync();

        if (demos.Count > 0)
        {
            db.AtsScrapedListings.RemoveRange(demos);
            await db.SaveChangesAsync();
            logger.LogInformation("ATS purged {Count} demo listings from overview.", demos.Count);
        }

        if (!await db.PlatformLogs.AnyAsync(l => l.Category == "Seed" && l.Message == DemoPurgeMarker))
        {
            db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Seed",
                Message = DemoPurgeMarker,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    private static AtsScrapeSource[] BuildSources() =>
    [
        new()
        {
            Id = SourceId(1),
            Name = "Gemeente Den Haag",
            // Canonical careers site (werkenbij.denhaag.nl has no DNS).
            Domain = "werkenvoor.denhaag.nl",
            ListUrl = "https://werkenvoor.denhaag.nl/vacatures/",
            DefaultLatitude = 52.0705,
            DefaultLongitude = 4.3007,
            DefaultLocationLabel = "Den Haag",
            IsEnabled = true
        },
        new()
        {
            // Kept for stable id continuity; disabled — host does not resolve.
            Id = SourceId(2),
            Name = "Gemeente Westland (werkenbij — uitgeschakeld)",
            Domain = "werkenbij.gemeentewestland.nl",
            ListUrl = "https://werkenbij.gemeentewestland.nl/",
            DefaultLatitude = 51.9917,
            DefaultLongitude = 4.2175,
            DefaultLocationLabel = "Westland",
            IsEnabled = false
        },
        new()
        {
            Id = SourceId(3),
            Name = "Gemeente Westland",
            Domain = "www.gemeentewestland.nl",
            ListUrl =
                "https://www.gemeentewestland.nl/bestuur-en-organisatie/over-de-organisatie/werken-voor-westland/vacatures",
            DefaultLatitude = 51.9917,
            DefaultLongitude = 4.2175,
            DefaultLocationLabel = "Westland",
            IsEnabled = true
        },
        new()
        {
            Id = SourceId(4),
            Name = "HTM (Den Haag)",
            Domain = "www.htm.nl",
            ListUrl = "https://www.htm.nl/over-htm/werken-bij-htm",
            DefaultLatitude = 52.0705,
            DefaultLongitude = 4.3007,
            DefaultLocationLabel = "Den Haag",
            IsEnabled = true
        },
        new()
        {
            Id = SourceId(5),
            Name = "HagaZiekenhuis",
            Domain = "www.hagaziekenhuis.nl",
            ListUrl = "https://www.hagaziekenhuis.nl/werken-bij-haga",
            DefaultLatitude = 52.058,
            DefaultLongitude = 4.288,
            DefaultLocationLabel = "Den Haag",
            IsEnabled = true
        }
    ];
}
