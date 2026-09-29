using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Demo/training-provider seed data for Development and Test only.
/// Never run against production/acceptatie — see <see cref="Services.TrainingUpskillService.EnsureDefaultsAsync"/>.
/// </summary>
public static class TrainingDemoSeed
{
    /// <summary>Fixed demo provider ids (migration deactivates these; do not delete).</summary>
    public static readonly Guid[] DemoProviderIds =
    [
        Guid.Parse("a11a0001-0001-4000-8000-000000000001"),
        Guid.Parse("a11a0001-0001-4000-8000-000000000002"),
        Guid.Parse("a11a0001-0001-4000-8000-000000000003"),
        Guid.Parse("a11a0001-0001-4000-8000-000000000004"),
        Guid.Parse("a11a0001-0001-4000-8000-000000000005"),
        Guid.Parse("a11a0001-0001-4000-8000-000000000006"),
    ];

    public static async Task EnsureAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.TrainingProviders.AnyAsync(cancellationToken))
        {
            foreach (var seed in Seeds())
            {
                db.TrainingProviders.Add(seed);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        await EnsureCompetencyWorkshopsAsync(db, cancellationToken);
        await EnsureOfferDeepLinksAsync(db, cancellationToken);
    }

    public static IEnumerable<TrainingProvider> Seeds()
    {
        var loi = new TrainingProvider
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000001"),
            Name = "LOI",
            Kind = TrainingProviderKind.NationalAffiliate,
            Network = TrainingNetwork.Daisycon,
            BaseUrl = "https://www.loi.nl/",
            FieldsCsv = "zorg,techniek,logistiek",
            Region = "Landelijk",
            CplEuro = 12m,
            CpaEuro = 75m,
            IsActive = true,
            SortOrder = 10
        };
        loi.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000011"),
            ProviderId = loi.Id,
            Title = "MBO Zorg & Welzijn (LOI)",
            FieldsCsv = "zorg",
            KeysCsv = "zorg,verpleeg,welzijn",
            ExternalPath = "/opleidingen/mbo/zorg-welzijn",
            IsActive = true,
            SortOrder = 1
        });
        loi.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000012"),
            ProviderId = loi.Id,
            Title = "Techniek & installatie (LOI)",
            FieldsCsv = "techniek",
            KeysCsv = "techniek,monteur,install",
            ExternalPath = "/opleidingen/mbo/techniek-installatie",
            IsActive = true,
            SortOrder = 2
        });
        loi.Offers.Add(LoiSkillOffer());

        var nti = new TrainingProvider
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000002"),
            Name = "NTI",
            Kind = TrainingProviderKind.NationalAffiliate,
            Network = TrainingNetwork.Awin,
            BaseUrl = "https://www.nti.nl/",
            FieldsCsv = "zorg,logistiek,techniek",
            Region = "Landelijk",
            CplEuro = 10m,
            CpaEuro = 70m,
            IsActive = true,
            SortOrder = 20
        };
        nti.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000021"),
            ProviderId = nti.Id,
            Title = "Logistiek & magazijn (NTI)",
            FieldsCsv = "logistiek",
            KeysCsv = "logistiek,magazijn,heftruck",
            ExternalPath = "/opleidingen/logistiek-magazijn",
            IsActive = true,
            SortOrder = 1
        });
        nti.Offers.Add(NtiSkillOffer());

        var zorg = new TrainingProvider
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000003"),
            Name = "Zorgcollege Haaglanden",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://www.rocmondriaan.nl/",
            FieldsCsv = "zorg",
            Region = "Den Haag / Westland",
            IntakeFeeEuro = 35m,
            StartFeeEuro = 150m,
            IsActive = true,
            SortOrder = 1
        };
        zorg.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000031"),
            ProviderId = zorg.Id,
            Title = "Helpende / Verzorgende IG — Haaglanden",
            FieldsCsv = "zorg",
            KeysCsv = "zorg,verpleeg,verzorg,helpende",
            ExternalPath = "/opleidingen/helpende-verzorgende-ig",
            IsActive = true,
            SortOrder = 1
        });

        var tech = new TrainingProvider
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000004"),
            Name = "Techniek College Westland",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://www.technischcollegewestland.nl/",
            FieldsCsv = "techniek",
            Region = "Westland",
            IntakeFeeEuro = 40m,
            StartFeeEuro = 175m,
            IsActive = true,
            SortOrder = 2
        };
        tech.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000041"),
            ProviderId = tech.Id,
            Title = "Monteur / installatietechniek Westland",
            FieldsCsv = "techniek",
            KeysCsv = "techniek,monteur,install,elektro",
            ExternalPath = "/opleidingen/monteur-installatietechniek",
            IsActive = true,
            SortOrder = 1
        });

        var log = new TrainingProvider
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000005"),
            Name = "Logistiek Academy Den Haag",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://www.s-bb.nl/",
            FieldsCsv = "logistiek",
            Region = "Den Haag",
            IntakeFeeEuro = 30m,
            StartFeeEuro = 140m,
            IsActive = true,
            SortOrder = 3
        };
        log.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000051"),
            ProviderId = log.Id,
            Title = "Magazijn & heftruck — Den Haag",
            FieldsCsv = "logistiek",
            KeysCsv = "logistiek,magazijn,heftruck,orderpick",
            ExternalPath = "/opleidingen/magazijn-heftruck-den-haag",
            IsActive = true,
            SortOrder = 1
        });

        return [zorg, tech, log, SkillsAcademy(), loi, nti];
    }

    public static TrainingOffer LoiSkillOffer() => new()
    {
        Id = Guid.Parse("a11a0001-0001-4000-8000-000000000013"),
        ProviderId = Guid.Parse("a11a0001-0001-4000-8000-000000000001"),
        Title = "Persoonlijke effectiviteit (LOI)",
        FieldsCsv = TrainingFieldCatalog.Vaardigheden,
        KeysCsv = "samenwerken,communicatie,resultaatgericht,stressbestendig,innovatie,klantcontact",
        ExternalPath = "/opleidingen/persoonlijke-effectiviteit",
        IsActive = true,
        SortOrder = 3
    };

    public static TrainingOffer NtiSkillOffer() => new()
    {
        Id = Guid.Parse("a11a0001-0001-4000-8000-000000000022"),
        ProviderId = Guid.Parse("a11a0001-0001-4000-8000-000000000002"),
        Title = "Communicatie & presenteren (NTI)",
        FieldsCsv = TrainingFieldCatalog.Vaardigheden,
        KeysCsv = "communicatie,presenteren,klantcontact,gastvrijheid",
        ExternalPath = "/opleidingen/communicatie-presenteren",
        IsActive = true,
        SortOrder = 2
    };

    public static TrainingProvider SkillsAcademy()
    {
        var academy = new TrainingProvider
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000006"),
            Name = "Praktijkacademie Haaglanden",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://www.rocmondriaan.nl/",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            Region = "Den Haag / Westland",
            IntakeFeeEuro = 25m,
            StartFeeEuro = 90m,
            IsActive = true,
            SortOrder = 0
        };
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000061"),
            ProviderId = academy.Id,
            Title = "Samenwerken en communiceren op de werkvloer",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "samenwerken,communicatie,teamoverleg,luisteren",
            ExternalPath = "/workshops/samenwerken-communiceren",
            IsActive = true,
            SortOrder = 1
        });
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000062"),
            ProviderId = academy.Id,
            Title = "Afronden en plannen onder druk",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "resultaatgericht,deadlines,organiseren,afronden",
            ExternalPath = "/workshops/afronden-plannen",
            IsActive = true,
            SortOrder = 2
        });
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000063"),
            ProviderId = academy.Id,
            Title = "Kalm blijven bij werkdruk",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "stressbestendig,weerbaarheid,werkdruk,kalm",
            ExternalPath = "/workshops/kalm-bij-werkdruk",
            IsActive = true,
            SortOrder = 3
        });
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000064"),
            ProviderId = academy.Id,
            Title = "Nieuwe manieren van werken",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "innovatie,probleemoplossen,digitale vaardigheden",
            ExternalPath = "/workshops/nieuwe-manieren-van-werken",
            IsActive = true,
            SortOrder = 4
        });
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000065"),
            ProviderId = academy.Id,
            Title = "Klantcontact en presenteren",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "klantcontact,presenteren,gastvrijheid,verkoop",
            ExternalPath = "/workshops/klantcontact-presenteren",
            IsActive = true,
            SortOrder = 5
        });
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000066"),
            ProviderId = academy.Id,
            Title = "Besluiten en tempo op de werkvloer",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "leiding,besluiten,tempo,aanpakken",
            ExternalPath = "/workshops/besluiten-tempo",
            IsActive = true,
            SortOrder = 6
        });
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000067"),
            ProviderId = academy.Id,
            Title = "Kwaliteit en checklists",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "nauwkeurig,kwaliteit,administratie,checklists",
            ExternalPath = "/workshops/kwaliteit-checklists",
            IsActive = true,
            SortOrder = 7
        });
        academy.Offers.Add(new TrainingOffer
        {
            Id = Guid.Parse("a11a0001-0001-4000-8000-000000000068"),
            ProviderId = academy.Id,
            Title = "Ritme en samenwerken in de ploeg",
            FieldsCsv = TrainingFieldCatalog.Vaardigheden,
            KeysCsv = "ritme,samenwerken,teamoverleg,rust",
            ExternalPath = "/workshops/ritme-samenwerken",
            IsActive = true,
            SortOrder = 8
        });
        return academy;
    }

    private static async Task EnsureCompetencyWorkshopsAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken)
    {
        var academy = SkillsAcademy();
        if (!await db.TrainingProviders.AnyAsync(p => p.Id == academy.Id, cancellationToken))
        {
            db.TrainingProviders.Add(academy);
        }
        else
        {
            var existing = await db.TrainingOffers
                .Where(o => o.ProviderId == academy.Id)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            foreach (var offer in academy.Offers)
            {
                if (!existing.Contains(offer.Id))
                {
                    db.TrainingOffers.Add(offer);
                }
            }
        }

        await AddOfferIfMissingAsync(db, LoiSkillOffer(), cancellationToken);
        await AddOfferIfMissingAsync(db, NtiSkillOffer(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureOfferDeepLinksAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken)
    {
        var pathById = Seeds()
            .SelectMany(p => p.Offers)
            .Concat(SkillsAcademy().Offers)
            .Concat([LoiSkillOffer(), NtiSkillOffer()])
            .Where(o => !string.IsNullOrWhiteSpace(o.ExternalPath))
            .GroupBy(o => o.Id)
            .ToDictionary(g => g.Key, g => g.First().ExternalPath!);

        var offers = await db.TrainingOffers
            .Where(o => o.ExternalPath == null || o.ExternalPath == "" || o.ExternalPath == "/")
            .ToListAsync(cancellationToken);
        var dirty = false;
        foreach (var offer in offers)
        {
            if (pathById.TryGetValue(offer.Id, out var path))
            {
                offer.ExternalPath = path;
                offer.UpdatedAtUtc = DateTime.UtcNow;
                dirty = true;
            }
        }

        if (dirty)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task AddOfferIfMissingAsync(
        JobsyDbContext db,
        TrainingOffer offer,
        CancellationToken cancellationToken)
    {
        if (await db.TrainingOffers.AnyAsync(o => o.Id == offer.Id, cancellationToken))
        {
            return;
        }

        if (!await db.TrainingProviders.AnyAsync(p => p.Id == offer.ProviderId, cancellationToken))
        {
            return;
        }

        db.TrainingOffers.Add(offer);
    }
}
