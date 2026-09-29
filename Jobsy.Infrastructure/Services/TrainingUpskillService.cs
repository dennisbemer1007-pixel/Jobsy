using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts;
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
        if (!AllowDemoSeed())
        {
            return;
        }

        await TrainingDemoSeed.EnsureAsync(_db, cancellationToken);
    }

    private bool AllowDemoSeed()
    {
        if (_environment.IsDevelopment())
        {
            return true;
        }

        var name = _environment.EnvironmentName;
        if (string.Equals(name, "Test", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _configuration.GetValue("Training:SeedDemoProviders", false);
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

    public async Task<IReadOnlyList<PassportCourseCardDto>> RecommendPassportAsync(
        Guid userId,
        string? searchBlob,
        IReadOnlyList<string>? searchKeys,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var keys = searchKeys?.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToList() ?? [];
        var learningGoals = await LoadLearningGoalsAsync(userId, cancellationToken);
        var blob = string.Join(' ', keys.Prepend(searchBlob ?? "").Where(s => !string.IsNullOrWhiteSpace(s)));
        var fields = TrainingFieldCatalog.Detect(keys.Prepend(searchBlob ?? "").Concat(learningGoals));

        var rows = await _db.TrainingOffers.AsNoTracking()
            .Include(o => o.Provider)
            .Where(o => o.IsActive && o.Provider.IsActive && o.ShowInPassport)
            .ToListAsync(cancellationToken);

        var slots = CourseSlotRules.Pick(
            rows,
            new CourseSlotRules.Context(fields, blob, learningGoals));
        return slots.Select(ToPassportCard).ToList();
    }

    private async Task<IReadOnlyList<string>> LoadLearningGoalsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var prefs = System.Text.Json.JsonSerializer.Deserialize<CandidatePreferencesDto>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return prefs?.LearningGoals?
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .Take(DiscoveryCatalogs.MaxLearningGoals)
                .ToList() ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
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
        var medium = offer.IsPartner || offer.Provider.Kind == TrainingProviderKind.RegionalPartner
            ? "partner"
            : "affiliate";
        var target = TrainingDeepLinkRules.Combine(offer.Provider.BaseUrl, offer.ExternalPath);
        var outbound = TrainingTracking.AppendParameters(
            target,
            hash,
            clickId,
            campaignValue,
            medium,
            offer.AffiliateCode);
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
        row.Type = request.Type;
        row.DurationValue = request.DurationValue;
        row.DurationUnit = request.DurationUnit;
        row.Delivery = request.Delivery;
        row.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        row.IsFree = request.IsFree;
        row.IsPartner = request.IsPartner;
        row.AffiliateCode = string.IsNullOrWhiteSpace(request.AffiliateCode) ? null : request.AffiliateCode.Trim();
        row.ShowInPassport = request.ShowInPassport;

        var provider = await _db.TrainingProviders.AsNoTracking()
            .FirstAsync(p => p.Id == request.ProviderId, cancellationToken);
        var deepLink = TrainingDeepLinkRules.Combine(provider.BaseUrl, path);
        var flagError = CourseSlotRules.ValidateFlags(row.IsFree, row.IsPartner, row.AffiliateCode, deepLink);
        if (flagError is not null)
        {
            throw new ArgumentException(flagError);
        }

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

    private static PassportCourseCardDto ToPassportCard(CourseSlotRules.Slot slot)
    {
        var o = slot.Offer;
        return new(
            o.Id,
            o.Title,
            o.Provider.Name,
            o.Type.ToString(),
            o.DurationValue,
            o.DurationUnit?.ToString(),
            o.Delivery.ToString(),
            o.Location,
            o.IsFree,
            o.IsPartner,
            TrainingTracking.RelFor(o.IsPartner));
    }

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
        => new(
            offer.Id,
            offer.ProviderId,
            offer.Title,
            offer.FieldsCsv,
            offer.KeysCsv,
            offer.ExternalPath,
            offer.IsActive,
            offer.SortOrder,
            offer.Type.ToString(),
            offer.DurationValue,
            offer.DurationUnit?.ToString(),
            offer.Delivery.ToString(),
            offer.Location,
            offer.IsFree,
            offer.IsPartner,
            offer.AffiliateCode,
            offer.ShowInPassport);

    private static string Csv(string value)
        => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
