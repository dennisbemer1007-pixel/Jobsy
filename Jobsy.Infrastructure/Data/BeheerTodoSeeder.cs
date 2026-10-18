using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

/// <summary>Idempotent seed for default admin to-do rows (mirrors migration <c>AddBeheerTodos</c>).</summary>
public static class BeheerTodoSeeder
{
    public static async Task SeedAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var seedAt = new DateTime(2026, 10, 13, 16, 0, 0, DateTimeKind.Utc);
        var rows = new[]
        {
            new BeheerTodo
            {
                Id = Guid.Parse("b1e0e0e0-e0e0-4000-8000-000000000001"),
                SeedKey = "seed-cotan-nip-mail",
                Title = "Mail NIP: wanneer gaat COTAN weer open?",
                Body = CotanNipMailBody.Trim(),
                Priority = BeheerTodoPriority.Hoog,
                Status = BeheerTodoStatus.Open,
                Category = "COTAN",
                CreatedAtUtc = seedAt,
                UpdatedAtUtc = seedAt,
            },
            new BeheerTodo
            {
                Id = Guid.Parse("b1e0e0e0-e0e0-4000-8000-000000000002"),
                SeedKey = "seed-render-encryption-keys",
                Title = "Encryptiesleutels op Render zetten (acc + prod, API + Web) zodat PR #638 kan mergen",
                Body = string.Empty,
                Priority = BeheerTodoPriority.Normaal,
                Status = BeheerTodoStatus.Open,
                Category = "Infra",
                CreatedAtUtc = seedAt,
                UpdatedAtUtc = seedAt,
            },
            new BeheerTodo
            {
                Id = Guid.Parse("b1e0e0e0-e0e0-4000-8000-000000000003"),
                SeedKey = "seed-acc-feature-flags",
                Title = "Flags aanzetten op acceptatie: ProactiveMatchingEnabled, EmployersEnabled, MatchCalibrationV1Enabled, TestsV2Enabled, TestItemExamplesEnabled",
                Body = string.Empty,
                Priority = BeheerTodoPriority.Normaal,
                Status = BeheerTodoStatus.Open,
                Category = "Acceptatie",
                CreatedAtUtc = seedAt,
                UpdatedAtUtc = seedAt,
            },
        };

        foreach (var row in rows)
        {
            var exists = await db.BeheerTodos.AnyAsync(t => t.SeedKey == row.SeedKey, cancellationToken);
            if (!exists)
            {
                db.BeheerTodos.Add(row);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    internal const string CotanNipMailBody =
        """
        Aan: directie@nip.nl
        Onderwerp: Vraag over hervatting COTAN-beoordelingen voor nieuwe instrumenten

        Geachte directie,

        Ik ben oprichter van Lobsy, een Nederlands loopbaanplatform voor werkzoekenden. Daarvoor ontwikkelen wij vragenlijsten voor competenties, beroepsinteresses (RIASEC), werkwaarden en cultuur/persoonlijkheid, beschikbaar in vijf talen. Bij de ontwikkeling volgen we het COTAN-beoordelingssysteem. De handleiding en het testmateriaal zijn gereed en de validatiestudie gaat binnenkort van start.

        Op uw website lees ik dat de COTAN voorlopig geen nieuwe instrumenten opneemt in de beoordelingsprocedure. Ik heb daarover twee vragen:

        1. Is er al een verwachting wanneer nieuwe aanmeldingen weer mogelijk zijn?
        2. Kunnen wij ons nu alvast registreren of op een wachtlijst plaatsen, zodat we kunnen indienen zodra dat weer kan?

        Verwijst u ons, zolang de COTAN geen nieuwe aanmeldingen aanneemt, bij voorkeur naar een andere manier om de kwaliteit van onze vragenlijsten onafhankelijk te laten toetsen? Dan horen wij dat ook graag.

        Alvast hartelijk dank voor uw reactie.

        Met vriendelijke groet,

        Dennis
        Lobsy, lobsy.nl
        """;
}
