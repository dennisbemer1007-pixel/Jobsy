using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Demo partner network for acceptatie/Development (Sanne · Zaandam · dierenarts scenario).
/// </summary>
public static class TrainingPartnerDemoSeed
{
    public static readonly Guid DemoNetworkId = Guid.Parse("b22b0001-0001-4000-8000-000000000001");

    public static async Task EnsureAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var provider = await db.TrainingProviders
            .Include(p => p.Offers)
            .FirstOrDefaultAsync(p => p.Id == DemoNetworkId, cancellationToken);
        if (provider is null)
        {
            provider = BuildProvider();
            db.TrainingProviders.Add(provider);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (provider.Offers.Count >= 8)
        {
            return;
        }

        foreach (var offer in BuildOffers(provider.Id).Where(o =>
                     provider.Offers.All(existing => existing.PartnerExternalId != o.PartnerExternalId)))
        {
            db.TrainingOffers.Add(offer);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static TrainingProvider BuildProvider() => new()
    {
        Id = DemoNetworkId,
        Name = "Leerwerkloket (regio)",
        Kind = TrainingProviderKind.RegionalPartner,
        Network = TrainingNetwork.Direct,
        BaseUrl = "https://partner-demo.lobsy.nl/",
        FieldsCsv = "zorg",
        Region = "Zaandam",
        IsActive = true,
        SortOrder = 1,
        FeedKind = TrainingPartnerFeedKind.None,
        ImportIntervalHours = 24,
        IsDemoPartner = true,
        Offers = BuildOffers(DemoNetworkId).ToList()
    };

    private static IEnumerable<TrainingOffer> BuildOffers(Guid providerId)
    {
        yield return Offer(providerId, "rust", "Rustig blijven als het druk wordt", TrainingOfferType.Workshop,
            2, TrainingDurationUnit.Days, TrainingDeliveryMode.OnSite, "Zaandam", true, null, null,
            TrainingOfferSection.Growth, CompetencyTestCatalog.Stressbestendigheid,
            "Groeipunt uit je test: rustig blijven onder druk. Handig aan een drukke balie.",
            "Growth point from your test: staying calm under pressure. Useful at a busy front desk.",
            ["Overzicht houden als alles tegelijk komt", "Rustig blijven bij een boze of verdrietige klant"],
            10, "/aanbod/rustig-druk");

        yield return Offer(providerId, "mbo", "Mbo 4 Dierenartsassistent paraveterinair (BBL)", TrainingOfferType.Opleiding,
            3, TrainingDurationUnit.Years, TrainingDeliveryMode.OnSite, "ROC", false, null, "Lesgeld per jaar",
            TrainingOfferSection.NextStep, null,
            "Dit is je volgende stap naar dierenarts. Leren en werken tegelijk.",
            "This is your next step toward veterinarian. Learn and work at the same time.",
            ["BBL: één dag school per week"], 1, "/aanbod/mbo-paraveterinair");

        yield return Offer(providerId, "basis", "Basis dierverzorging en hygiëne", TrainingOfferType.Cursus,
            6, TrainingDurationUnit.Weeks, TrainingDeliveryMode.Online, null, false, 49m, null,
            TrainingOfferSection.NextStep, null,
            "Met certificaat voor je Bewijzen. Sterk bij je aanmelding voor de BBL.",
            "Certificate for your proofs. Strong for your BBL application.",
            [], 3, "/aanbod/basis-dierverzorging");

        yield return Offer(providerId, "balie", "Lastige gesprekken aan de balie", TrainingOfferType.Cursus,
            4, TrainingDurationUnit.Weeks, TrainingDeliveryMode.Online, null, true, null, null,
            TrainingOfferSection.Growth, CompetencyTestCatalog.Stressbestendigheid,
            "Past bij stap 3: werken in een kliniek, waar je baasjes te woord staat.",
            "Fits step 3: working in a clinic where you talk to pet owners.",
            [], 11, "/aanbod/balie-gesprekken");

        yield return Offer(providerId, "plan", "Plannen en afmaken wat je begint", TrainingOfferType.Workshop,
            1, TrainingDurationUnit.Days, TrainingDeliveryMode.OnSite, "Zaandam", true, null, null,
            TrainingOfferSection.Growth, CompetencyTestCatalog.Resultaatgerichtheid,
            "Groeipunt uit je test: plannen en doorzetten tot iets af is.",
            "Growth point from your test: planning and finishing what you start.",
            [], 12, "/aanbod/plannen-afmaken");

        yield return Offer(providerId, "ehbd", "Eerste hulp bij dieren", TrainingOfferType.Workshop,
            1, TrainingDurationUnit.Days, TrainingDeliveryMode.OnSite, "Amsterdam", false, 95m, null,
            TrainingOfferSection.NextStep, null,
            "Vaak gevraagd in vacatures voor dierenartsassistent.",
            "Often asked for in vet assistant vacancies.",
            [], 2, "/aanbod/ehbd");

        yield return Offer(providerId, "digi", "Digitaal werken: mail, agenda en roosters", TrainingOfferType.Cursus,
            3, TrainingDurationUnit.Days, TrainingDeliveryMode.Blended, null, true, null, null,
            TrainingOfferSection.General, null,
            "Voor iedereen: handig in bijna elke baan, ook in de kliniek.",
            "For everyone: useful in almost any job, including the clinic.",
            [], 20, "/aanbod/digitaal-werken");

        yield return Offer(providerId, "taal", "Nederlands op de werkvloer", TrainingOfferType.Cursus,
            10, TrainingDurationUnit.Weeks, TrainingDeliveryMode.OnSite, "Zaandam", true, null, null,
            TrainingOfferSection.General, null,
            "Voor iedereen: zekerder praten met collega's en klanten.",
            "For everyone: talk more confidently with colleagues and customers.",
            [], 21, "/aanbod/nederlands-werkvloer");

        yield return Offer(providerId, "samen", "Feedback geven en vragen", TrainingOfferType.Workshop,
            1, TrainingDurationUnit.Days, TrainingDeliveryMode.Online, null, true, null, null,
            TrainingOfferSection.Growth, CompetencyTestCatalog.Samenwerken,
            "Bouwt voort op je sterke punt samenwerken.",
            "Builds on your strength in teamwork.",
            [], 13, "/aanbod/feedback");
    }

    private static TrainingOffer Offer(
        Guid providerId,
        string externalId,
        string title,
        TrainingOfferType type,
        int durationValue,
        TrainingDurationUnit durationUnit,
        TrainingDeliveryMode delivery,
        string? location,
        bool isFree,
        decimal? priceEuro,
        string? priceLabel,
        TrainingOfferSection section,
        string? competency,
        string reasonNl,
        string reasonEn,
        IReadOnlyList<string> learnBullets,
        int sort,
        string path)
        => new()
        {
            Id = DeterministicOfferId(externalId),
            ProviderId = providerId,
            PartnerExternalId = externalId,
            Title = title,
            Type = type,
            DurationValue = durationValue,
            DurationUnit = durationUnit,
            Delivery = delivery,
            Location = location,
            IsFree = isFree,
            PriceEuro = priceEuro,
            PriceLabel = priceLabel,
            Section = section,
            CompetenciesCsv = competency ?? "",
            KeysCsv = "dierenarts,dieren,zaandam,zorg",
            FieldsCsv = "zorg",
            ReasonNl = reasonNl,
            ReasonEn = reasonEn,
            LearnBulletsJson = learnBullets.Count == 0
                ? null
                : System.Text.Json.JsonSerializer.Serialize(learnBullets),
            ExternalPath = path,
            IsActive = true,
            IsPartner = true,
            ShowInPassport = section == TrainingOfferSection.Growth,
            SortOrder = sort,
            StartsOn = new DateOnly(2026, 11, 12)
        };

    private static Guid DeterministicOfferId(string key)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("lobsy-training-offer:" + key));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, 16);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
