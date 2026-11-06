using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>Dev demo: full external vacancy for Sanne (PR captures / UAT).</summary>
public static class ExternalVacancyDemoSeeder
{
    public static readonly Guid DemoExternalVacancyId = Guid.Parse("eeeeeeee-2222-2222-2222-222222222222");
    private static readonly Guid SanneUserId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    private static readonly string[] DemoRequirements =
    [
        "MBO-niveau 4 retail of vergelijkbaar",
        "Minimaal 2 jaar leidinggevende ervaring in de detailhandel",
    ];

    private static readonly string[] DemoStrengths = ["Leidinggeven", "Retail", "Klantcontact"];
    private static readonly string[] DemoChallenges = ["Formele MBO-diploma", "Reistijd buiten Den Haag"];

    public static async Task EnsureAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.CandidateExternalVacancies.AnyAsync(v => v.Id == DemoExternalVacancyId))
        {
            return;
        }

        var sanne = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == SanneUserId);
        if (sanne is null)
        {
            return;
        }

        var requirements = JsonSerializer.Serialize(DemoRequirements);

        var facts = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["bedrijf"] = "Intertoys",
            ["plaats"] = "Waldorpstraat 164, Den Haag",
            ["uren"] = "36 uur",
            ["salaris"] = "marktconform salaris + 8% vakantiegeld",
            ["start"] = "",
            ["opleiding"] = "",
        });

        var insights = JsonSerializer.Serialize(new
        {
            strengths = DemoStrengths,
            challenges = DemoChallenges,
        });

        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = DemoExternalVacancyId,
            CandidateUserId = SanneUserId,
            SourceUrl = "https://www.intertoys.nl/vacatures/bedrijfsleider-den-haag-demo",
            SourceHost = "intertoys.nl",
            Title = "Bedrijfsleider",
            CompanyName = "Intertoys",
            Place = "Waldorpstraat 164, Den Haag",
            HoursText = "36 uur",
            PayText = "marktconform salaris + 8% vakantiegeld",
            StartText = "",
            TrainingText = "",
            RequirementsBulletsJson = requirements,
            StructuredFactsJson = facts,
            MatchInsightsJson = insights,
            TravelMinutesEstimate = null,
            Status = CandidateExternalVacancyStatus.Saved,
            SavedAtUtc = DateTime.UtcNow.AddDays(-2),
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded demo external vacancy {Id} for {Email}", DemoExternalVacancyId, sanne.Email);
    }
}
