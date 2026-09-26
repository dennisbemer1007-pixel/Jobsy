using System.Globalization;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure.Services;

public sealed class TrainingUpskillService : ITrainingUpskillService
{
    public const string DefaultTrackingSecret = "lobsy-training-tracking-dev";

    private readonly JobsyDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public TrainingUpskillService(
        JobsyDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        if (!await _db.TrainingProviders.AnyAsync(cancellationToken))
        {
            foreach (var seed in Seeds())
            {
                _db.TrainingProviders.Add(seed);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        await EnsureCompetencyWorkshopsAsync(cancellationToken);
        await EnsureOfferDeepLinksAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingOfferCardDto>> RecommendAsync(
        Guid userId,
        string? jobTitle,
        IReadOnlyList<string>? searchKeys,
        string campaign,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        _ = userId;
        _ = campaign;

        var title = RoleFitCheckBuilder.NormalizeTitle(jobTitle) ?? "";
        var keys = searchKeys?.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToList() ?? [];
        var blob = string.Join(' ', keys.Prepend(title));
        var fields = TrainingFieldCatalog.Detect(keys.Prepend(title));

        var rows = await _db.TrainingOffers.AsNoTracking()
            .Include(o => o.Provider)
            .Where(o => o.IsActive && o.Provider.IsActive)
            .ToListAsync(cancellationToken);

        return rows
            .Select(o => (
                Offer: o,
                Score: TrainingMatchRules.Score(
                    o.Provider.Kind,
                    TrainingMatchRules.SplitCsv(o.FieldsCsv),
                    TrainingMatchRules.SplitCsv(o.KeysCsv),
                    fields,
                    blob)))
            .Where(x => x.Score > 0)
            .Where(x => TrainingDeepLinkRules.TryCombine(x.Offer.Provider.BaseUrl, x.Offer.ExternalPath, out _))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Offer.SortOrder)
            .Take(TrainingMatchRules.MaxResults)
            .Select(x => ToCard(x.Offer, campaign))
            .ToList();
    }

    public async Task<TrainingTrackedLinkDto> TrackAsync(
        Guid userId,
        Guid offerId,
        string? campaign,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var offer = await _db.TrainingOffers
            .Include(o => o.Provider)
            .FirstOrDefaultAsync(o => o.Id == offerId && o.IsActive && o.Provider.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Opleiding niet gevonden.");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new InvalidOperationException("Gebruiker niet gevonden.");

        var secret = TrackingSecret();
        var hash = TrainingTracking.CandidateHash(userId, secret);
        var clickId = Guid.NewGuid();
        var campaignValue = string.IsNullOrWhiteSpace(campaign) ? TrainingTracking.CampaignFit : campaign.Trim();
        var medium = offer.Provider.Kind == TrainingProviderKind.RegionalPartner ? "partner" : "affiliate";
        var target = TrainingDeepLinkRules.Combine(offer.Provider.BaseUrl, offer.ExternalPath);
        var outbound = TrainingTracking.AppendParameters(target, hash, clickId, campaignValue, medium);
        if (!TrainingTracking.LooksSafeOutbound(outbound) || !TrainingDeepLinkRules.IsCourseDeepLink(outbound))
        {
            throw new InvalidOperationException("Ongeldige opleiders-deeplink (geen homepage).");
        }

        _db.TrainingClicks.Add(new TrainingClick
        {
            Id = clickId,
            OfferId = offer.Id,
            UserId = userId,
            CandidateHash = hash,
            EmailHash = TrainingTracking.EmailHash(user.Email, secret),
            Campaign = campaignValue,
            ClickedAtUtc = DateTime.UtcNow,
            OutboundUrl = outbound
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new TrainingTrackedLinkDto(clickId, outbound, hash);
    }

    public async Task<IReadOnlyList<TrainingProviderAdminDto>> ListProvidersAdminAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var rows = await _db.TrainingProviders.AsNoTracking()
            .Include(p => p.Offers)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(ToAdmin).ToList();
    }

    public async Task<TrainingProviderAdminDto> UpsertProviderAsync(
        TrainingProviderUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? "").Trim();
        if (name.Length < 2)
        {
            throw new ArgumentException("Naam is verplicht.");
        }

        var url = (request.BaseUrl ?? "").Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("https" or "http"))
        {
            throw new ArgumentException("Zet een geldige https-URL voor de opleider.");
        }

        TrainingProvider row;
        if (request.Id is Guid id)
        {
            row = await _db.TrainingProviders.Include(p => p.Offers)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException("Opleider niet gevonden.");
        }
        else
        {
            row = new TrainingProvider { Id = Guid.NewGuid() };
            _db.TrainingProviders.Add(row);
        }

        row.Name = name;
        row.Kind = request.Kind;
        row.Network = request.Network;
        row.BaseUrl = uri.ToString();
        row.FieldsCsv = TrainingMatchRules.JoinCsv(TrainingMatchRules.SplitCsv(request.FieldsCsv));
        row.Region = (request.Region ?? "").Trim();
        row.CplEuro = request.CplEuro;
        row.CpaEuro = request.CpaEuro;
        row.IntakeFeeEuro = request.IntakeFeeEuro;
        row.StartFeeEuro = request.StartFeeEuro;
        row.IsActive = request.IsActive;
        row.SortOrder = request.SortOrder;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToAdmin(row);
    }

    public async Task DeleteProviderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.TrainingProviders.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                  ?? throw new KeyNotFoundException("Opleider niet gevonden.");
        _db.TrainingProviders.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<TrainingOfferAdminDto> UpsertOfferAsync(
        TrainingOfferUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = (request.Title ?? "").Trim();
        if (title.Length < 2)
        {
            throw new ArgumentException("Titel is verplicht.");
        }

        var providerExists = await _db.TrainingProviders.AnyAsync(p => p.Id == request.ProviderId, cancellationToken);
        if (!providerExists)
        {
            throw new KeyNotFoundException("Opleider niet gevonden.");
        }

        TrainingOffer row;
        if (request.Id is Guid id)
        {
            row = await _db.TrainingOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                  ?? throw new KeyNotFoundException("Opleiding niet gevonden.");
        }
        else
        {
            row = new TrainingOffer { Id = Guid.NewGuid(), ProviderId = request.ProviderId };
            _db.TrainingOffers.Add(row);
        }

        row.ProviderId = request.ProviderId;
        row.Title = title;
        row.FieldsCsv = TrainingMatchRules.JoinCsv(TrainingMatchRules.SplitCsv(request.FieldsCsv));
        row.KeysCsv = TrainingMatchRules.JoinCsv(TrainingMatchRules.SplitCsv(request.KeysCsv));
        var path = TrainingDeepLinkRules.NormalizeExternalPath(request.ExternalPath)
                   ?? throw new ArgumentException(
                       "Zet een directe cursus-deeplink (pad of volledige URL). Geen homepage.");
        row.ExternalPath = path;
        row.IsActive = request.IsActive;
        row.SortOrder = request.SortOrder;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToOfferAdmin(row);
    }

    public async Task DeleteOfferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.TrainingOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                  ?? throw new KeyNotFoundException("Opleiding niet gevonden.");
        _db.TrainingOffers.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<TrainingConversionDto> RecordConversionAsync(
        TrainingConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        TrainingClick? click = null;
        if (request.ClickId is Guid clickId)
        {
            click = await _db.TrainingClicks.FirstOrDefaultAsync(c => c.Id == clickId, cancellationToken);
        }

        if (click is null && !string.IsNullOrWhiteSpace(request.CandidateHash))
        {
            var hash = request.CandidateHash.Trim().ToLowerInvariant();
            click = await _db.TrainingClicks
                .Where(c => c.CandidateHash == hash)
                .OrderByDescending(c => c.ClickedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (click is null && !string.IsNullOrWhiteSpace(request.Email))
        {
            var emailHash = TrainingTracking.EmailHash(request.Email, TrackingSecret());
            click = await _db.TrainingClicks
                .Where(c => c.EmailHash == emailHash)
                .OrderByDescending(c => c.ClickedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (click is null)
        {
            throw new KeyNotFoundException("Geen klik gevonden om te matchen.");
        }

        var existing = await _db.TrainingConversions
            .FirstOrDefaultAsync(c => c.ClickId == click.Id && c.Kind == request.Kind, cancellationToken);
        if (existing is not null)
        {
            return new TrainingConversionDto(existing.Id, existing.ClickId, existing.Kind.ToString(), existing.RecordedAtUtc, existing.Source);
        }

        var source = request.ClickId is not null
            ? "click"
            : !string.IsNullOrWhiteSpace(request.Email) ? "email" : "hash";
        var row = new TrainingConversion
        {
            Id = Guid.NewGuid(),
            ClickId = click.Id,
            Kind = request.Kind,
            RecordedAtUtc = DateTime.UtcNow,
            Source = source
        };
        _db.TrainingConversions.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return new TrainingConversionDto(row.Id, row.ClickId, row.Kind.ToString(), row.RecordedAtUtc, row.Source);
    }

    public async Task<string> ExportCsvAsync(
        int year,
        int month,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var clicks = await _db.TrainingClicks.AsNoTracking()
            .Include(c => c.Offer).ThenInclude(o => o.Provider)
            .Include(c => c.Conversions)
            .Where(c => c.ClickedAtUtc >= start && c.ClickedAtUtc < end)
            .Where(c => providerId == null || c.Offer.ProviderId == providerId)
            .OrderBy(c => c.ClickedAtUtc)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("clickedAtUtc,provider,kind,network,offer,candidateHash,campaign,leads,started,feeEuro,reconcileVia");
        foreach (var click in clicks)
        {
            var provider = click.Offer.Provider;
            var leads = click.Conversions.Count(c => c.Kind == TrainingConversionKind.Lead);
            var started = click.Conversions.Count(c => c.Kind == TrainingConversionKind.Started);
            var fee = started > 0
                ? provider.StartFeeEuro ?? provider.CpaEuro
                : leads > 0
                    ? provider.IntakeFeeEuro ?? provider.CplEuro
                    : null;
            var via = provider.Kind == TrainingProviderKind.NationalAffiliate
                ? provider.Network.ToString()
                : "lobsy-export";
            sb.Append(click.ClickedAtUtc.ToString("o", CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(provider.Name)).Append(',')
                .Append(provider.Kind).Append(',')
                .Append(provider.Network).Append(',')
                .Append(Csv(click.Offer.Title)).Append(',')
                .Append(click.CandidateHash).Append(',')
                .Append(Csv(click.Campaign)).Append(',')
                .Append(leads).Append(',')
                .Append(started).Append(',')
                .Append(fee?.ToString(CultureInfo.InvariantCulture) ?? "").Append(',')
                .Append(via)
                .AppendLine();
        }

        return sb.ToString();
    }

    public async Task ForgetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var clicks = await _db.TrainingClicks.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        foreach (var click in clicks)
        {
            click.UserId = null;
        }

        if (clicks.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private string TrackingSecret()
    {
        var configured = _configuration["Training:TrackingSecret"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        if (_environment.IsDevelopment())
        {
            return DefaultTrackingSecret;
        }

        throw new InvalidOperationException(
            "Training:TrackingSecret is verplicht buiten Development. " +
            "Zet Training__TrackingSecret op een lange willekeurige geheime waarde.");
    }

    private static string CombineUrl(string baseUrl, string? path)
        => TrainingDeepLinkRules.Combine(baseUrl, path);

    private static TrainingOfferCardDto ToCard(TrainingOffer offer, string? campaign = null)
    {
        var skill = string.Equals(campaign?.Trim(), TrainingTracking.CampaignCompetence, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(campaign?.Trim(), TrainingTracking.CampaignDisc, StringComparison.OrdinalIgnoreCase);
        return new(
            offer.Id,
            offer.Title,
            offer.Provider.Name,
            offer.Provider.Kind.ToString(),
            offer.Provider.Network.ToString(),
            offer.Provider.Region,
            skill ? TrainingCopy.SkillCta : TrainingCopy.Cta,
            skill ? TrainingCopy.SkillAdvice : TrainingCopy.GapAdvice);
    }

    private static TrainingProviderAdminDto ToAdmin(TrainingProvider provider)
        => new(
            provider.Id,
            provider.Name,
            provider.Kind.ToString(),
            provider.Network.ToString(),
            provider.BaseUrl,
            provider.FieldsCsv,
            provider.Region,
            provider.CplEuro,
            provider.CpaEuro,
            provider.IntakeFeeEuro,
            provider.StartFeeEuro,
            provider.IsActive,
            provider.SortOrder,
            provider.Offers.OrderBy(o => o.SortOrder).Select(ToOfferAdmin).ToList());

    private static TrainingOfferAdminDto ToOfferAdmin(TrainingOffer offer)
        => new(offer.Id, offer.ProviderId, offer.Title, offer.FieldsCsv, offer.KeysCsv, offer.ExternalPath, offer.IsActive, offer.SortOrder);

    private static string Csv(string value)
        => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static IEnumerable<TrainingProvider> Seeds()
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

    private async Task EnsureCompetencyWorkshopsAsync(CancellationToken cancellationToken)
    {
        var academy = SkillsAcademy();
        if (!await _db.TrainingProviders.AnyAsync(p => p.Id == academy.Id, cancellationToken))
        {
            _db.TrainingProviders.Add(academy);
        }
        else
        {
            var existing = await _db.TrainingOffers
                .Where(o => o.ProviderId == academy.Id)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            foreach (var offer in academy.Offers)
            {
                if (!existing.Contains(offer.Id))
                {
                    _db.TrainingOffers.Add(offer);
                }
            }
        }

        await AddOfferIfMissingAsync(LoiSkillOffer(), cancellationToken);
        await AddOfferIfMissingAsync(NtiSkillOffer(), cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOfferDeepLinksAsync(CancellationToken cancellationToken)
    {
        var pathById = Seeds()
            .SelectMany(p => p.Offers)
            .Concat(SkillsAcademy().Offers)
            .Concat([LoiSkillOffer(), NtiSkillOffer()])
            .Where(o => !string.IsNullOrWhiteSpace(o.ExternalPath))
            .GroupBy(o => o.Id)
            .ToDictionary(g => g.Key, g => g.First().ExternalPath!);

        var offers = await _db.TrainingOffers
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
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task AddOfferIfMissingAsync(TrainingOffer offer, CancellationToken cancellationToken)
    {
        if (await _db.TrainingOffers.AnyAsync(o => o.Id == offer.Id, cancellationToken))
        {
            return;
        }

        if (!await _db.TrainingProviders.AnyAsync(p => p.Id == offer.ProviderId, cancellationToken))
        {
            return;
        }

        _db.TrainingOffers.Add(offer);
    }

    private static TrainingOffer LoiSkillOffer() => new()
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

    private static TrainingOffer NtiSkillOffer() => new()
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

    private static TrainingProvider SkillsAcademy()
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
}
