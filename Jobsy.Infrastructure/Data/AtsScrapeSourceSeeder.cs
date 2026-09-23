using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent whitelist of direct local employer career sites (no agencies / aggregators).
/// </summary>
internal static class AtsScrapeSourceSeeder
{
    private const string SeedMarker = "ATS scrape source whitelist v1";

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

    private static AtsScrapeSource[] BuildSources() =>
    [
        new()
        {
            Id = SourceId(1),
            Name = "Gemeente Den Haag",
            Domain = "werkenbij.denhaag.nl",
            ListUrl = "https://werkenbij.denhaag.nl/vacatures",
            DefaultLatitude = 52.0705,
            DefaultLongitude = 4.3007,
            DefaultLocationLabel = "Den Haag",
            IsEnabled = true
        },
        new()
        {
            Id = SourceId(2),
            Name = "Gemeente Westland",
            Domain = "werkenbij.gemeentewestland.nl",
            ListUrl = "https://werkenbij.gemeentewestland.nl/",
            DefaultLatitude = 51.9917,
            DefaultLongitude = 4.2175,
            DefaultLocationLabel = "Westland",
            IsEnabled = true
        },
        new()
        {
            Id = SourceId(3),
            Name = "Gemeente Westland (alternatief)",
            Domain = "www.gemeentewestland.nl",
            ListUrl = "https://www.gemeentewestland.nl/werken-bij",
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
            Domain = "werkenbijhagaziekenhuis.nl",
            ListUrl = "https://werkenbijhagaziekenhuis.nl/vacatures",
            DefaultLatitude = 52.058,
            DefaultLongitude = 4.288,
            DefaultLocationLabel = "Den Haag",
            IsEnabled = true
        }
    ];
}
