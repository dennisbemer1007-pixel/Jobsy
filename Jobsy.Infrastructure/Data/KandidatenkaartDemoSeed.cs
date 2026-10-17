using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Local/demo candidates for kandidatenkaart evidence (test accounts only).
/// </summary>
public static class KandidatenkaartDemoSeed
{
    public const string SeedMarker = "kandidatenkaart-demo-v1";

    private static readonly (string Key, string Label, double Lat, double Lng, int Count)[] Areas =
    [
        ("amersfoort-leusden", "Amersfoort en Leusden", 52.1561, 5.3878, 42),
        ("soest-baarn", "Soest en Baarn", 52.1734, 5.2914, 38),
        ("hilversum", "Hilversum en omgeving", 52.2292, 5.1669, 35),
        ("utrecht-zuid", "Utrecht-Zuid", 52.0705, 5.1115, 31),
        ("barneveld", "Barneveld", 52.1400, 5.5847, 18),
    ];

    public static IReadOnlyList<(string Key, string Label, double Lat, double Lng, int Count)> DemoAreas => Areas;

    public static async Task EnsureAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var marker = await db.PlatformFeatureSettings.AsNoTracking()
            .Select(s => s.PublicWebBaseUrl)
            .FirstOrDefaultAsync(cancellationToken);
        _ = marker;

        var existing = await db.Users.CountAsync(
            u => u.IsTestAccount && u.Email.StartsWith("kk-demo-"),
            cancellationToken);
        if (existing >= 120)
        {
            return;
        }

        var rng = new Random(707);
        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var area in Areas)
        {
            for (var i = 0; i < area.Count; i++)
            {
                var email = $"kk-demo-{area.Key}-{i:D3}@lobsy.local.test";
                if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
                {
                    continue;
                }

                var lat = area.Lat + (rng.NextDouble() - 0.5) * 0.08;
                var lng = area.Lng + (rng.NextDouble() - 0.5) * 0.12;
                var match = rng.Next(58, 96);

                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FullName = $"Demo kandidaat {area.Key}-{i}",
                    Role = UserRole.Candidate,
                    OpenForWork = true,
                    IsActive = true,
                    IsTestAccount = true,
                    EmailVerifiedAtUtc = now,
                    LastLoginAtUtc = now.AddDays(-rng.Next(0, 10)),
                    TalentPoolConsentAt = now.AddMonths(-2),
                    TalentPoolConsentVersion = SeedMarker,
                    HomeLocation = new GeoPoint(lat, lng),
                    PreferencesJson = $$"""{"kkDemoArea":"{{area.Key}}","kkDemoMatch":{{match}}}""",
                    ReminderEmailsEnabled = true
                });
                created++;
            }
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
