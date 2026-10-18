using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure.Services;

public sealed class TrainingPartnerService : ITrainingPartnerService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly JobsyDbContext _db;
    private readonly TrainingPartnerFeedImporter _importer;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public TrainingPartnerService(
        JobsyDbContext db,
        TrainingPartnerFeedImporter importer,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _importer = importer;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task EnsureDemoPartnersAsync(CancellationToken cancellationToken = default)
    {
        if (!AllowDemoSeed())
        {
            return;
        }

        await TrainingPartnerDemoSeed.EnsureAsync(_db, cancellationToken);
        await ImportDueProvidersAsync(cancellationToken);
    }

    public async Task ImportDueProvidersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var providers = await _db.TrainingProviders
            .Where(p => p.IsActive && p.FeedKind != TrainingPartnerFeedKind.None && p.FeedUrl != null)
            .ToListAsync(cancellationToken);
        foreach (var provider in providers)
        {
            if (provider.IsDemoPartner && !AllowDemoSeed())
            {
                continue;
            }

            var due = provider.LastImportedAtUtc is null
                      || now - provider.LastImportedAtUtc.Value >= TimeSpan.FromHours(Math.Max(1, provider.ImportIntervalHours));
            if (due)
            {
                await _importer.ImportAsync(provider, cancellationToken);
            }
        }
    }

    public async Task ImportProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _db.TrainingProviders
            .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken)
            ?? throw new KeyNotFoundException("Partner not found.");
        await _importer.ImportAsync(provider, cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingPartnerOfferDto>> ListForCareerStepAsync(
        Guid userId,
        string? stepTitle,
        IReadOnlyList<string>? gapKeys,
        CancellationToken cancellationToken = default)
    {
        await EnsureDemoPartnersAsync(cancellationToken);
        var enrollments = await LoadEnrollmentMapAsync(userId, cancellationToken);
        var blob = string.Join(' ', (gapKeys ?? []).Prepend(stepTitle ?? "").Where(s => !string.IsNullOrWhiteSpace(s)));
        var fields = TrainingFieldCatalog.Detect((gapKeys ?? []).Prepend(stepTitle ?? ""));

        var rows = await ActiveOffersAsync(cancellationToken);
        var english = await IsEnglishAsync(userId, cancellationToken);

        return rows
            .Select(o => (Offer: o, Score: ScoreOffer(o, fields, blob, stepTitle)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Offer.SortOrder)
            .Take(6)
            .Select(x => ToDto(x.Offer, enrollments, english, stepTitle))
            .ToList();
    }

    public async Task<TrainingLearnGrowPageDto> GetLearnGrowPageAsync(
        Guid userId,
        TrainingLearnGrowQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureDemoPartnersAsync(cancellationToken);
        var enrollments = await LoadEnrollmentMapAsync(userId, cancellationToken);
        var english = await IsEnglishAsync(userId, cancellationToken);
        var scores = await LoadCompetencyScoresAsync(userId, cancellationToken);
        var dream = await LoadDreamTitleAsync(userId, cancellationToken);

        var rows = await ActiveOffersAsync(cancellationToken);
        rows = ApplyFilters(rows, query).ToList();

        var nextStepTitle = dream.Contains("dieren", StringComparison.OrdinalIgnoreCase)
            ? "Mbo 4 Dierenartsassistent paraveterinair"
            : dream;

        var dtos = rows.Select(o => ToDto(o, enrollments, english, nextStepTitle)).ToList();
        return new TrainingLearnGrowPageDto(
            dtos.Where(o => o.Section == TrainingOfferSection.NextStep).ToList(),
            dtos.Where(o => o.Section == TrainingOfferSection.Growth).ToList(),
            dtos.Where(o => o.Section == TrainingOfferSection.General).ToList(),
            dtos.Count,
            nextStepTitle,
            GrowthSubtitle(scores));
    }

    public async Task<IReadOnlyList<TrainingCompetencyGrowthDto>> GetCompetencyGrowthAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureDemoPartnersAsync(cancellationToken);
        var scores = await LoadCompetencyScoresAsync(userId, cancellationToken);
        if (scores is null)
        {
            return [];
        }

        var enrollments = await LoadEnrollmentMapAsync(userId, cancellationToken);
        var english = await IsEnglishAsync(userId, cancellationToken);
        var offers = await ActiveOffersAsync(cancellationToken);
        var dream = await LoadDreamTitleAsync(userId, cancellationToken);

        var list = new List<TrainingCompetencyGrowthDto>();
        foreach (var category in CompetencyTestCatalog.QuickScanCategories)
        {
            var score = ScoreForCategory(scores, category);
            if (score is null)
            {
                continue;
            }

            var value = score.Value;
            var bandKey = CompetencySkillBands.BandKey(value);
            var meaningKey = CompetencyTrainingCatalog.MeaningKey(category, value);
            var showOffers = CompetencySkillBands.ShowsInlineOffers(value);
            IReadOnlyList<TrainingPartnerOfferDto> matched = [];
            if (showOffers)
            {
                matched = offers
                    .Where(o => TrainingMatchRules.SplitCsv(o.CompetenciesCsv)
                        .Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase)))
                    .Take(3)
                    .Select(o => ToDto(o, enrollments, english, dream))
                    .ToList();
            }

            list.Add(new TrainingCompetencyGrowthDto(
                category,
                value,
                bandKey,
                meaningKey,
                showOffers && matched.Count > 0,
                matched));
        }

        return list;
    }

    public async Task<TrainingProgressSummaryDto> GetProgressAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Set<TrainingEnrollment>()
            .AsNoTracking()
            .Include(e => e.Offer)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = rows.Select(e => new TrainingProgressItemDto(
            e.OfferId,
            e.Offer.Title,
            e.Status,
            e.ProgressCurrent,
            e.ProgressTotal,
            e.VerifiedAtUtc is not null)).ToList();

        return new TrainingProgressSummaryDto(
            rows.Count(e => e.Status == TrainingEnrollmentStatus.InProgress),
            rows.Count(e => e.Status is TrainingEnrollmentStatus.Completed or TrainingEnrollmentStatus.Verified),
            rows.Count(e => e.Status == TrainingEnrollmentStatus.Saved),
            items);
    }

    public async Task ToggleSavedAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<TrainingEnrollment>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.OfferId == offerId, cancellationToken);
        if (row is null)
        {
            _db.Set<TrainingEnrollment>().Add(new TrainingEnrollment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OfferId = offerId,
                Status = TrainingEnrollmentStatus.Saved,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else if (row.Status == TrainingEnrollmentStatus.Saved)
        {
            _db.Set<TrainingEnrollment>().Remove(row);
        }
        else
        {
            row.Status = TrainingEnrollmentStatus.Saved;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetInProgressAsync(
        Guid userId,
        Guid offerId,
        int progressCurrent,
        int progressTotal,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<TrainingEnrollment>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.OfferId == offerId, cancellationToken);
        if (row is null)
        {
            row = new TrainingEnrollment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OfferId = offerId
            };
            _db.Set<TrainingEnrollment>().Add(row);
        }

        row.Status = TrainingEnrollmentStatus.InProgress;
        row.ProgressCurrent = Math.Max(0, progressCurrent);
        row.ProgressTotal = Math.Max(1, progressTotal);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestCertificateVerificationAsync(
        Guid userId,
        Guid offerId,
        string certificateTitle,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<TrainingEnrollment>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.OfferId == offerId, cancellationToken)
            ?? throw new KeyNotFoundException("Enrollment not found.");
        row.Status = TrainingEnrollmentStatus.Completed;
        row.CertificateTitle = certificateTitle.Trim();
        row.CompletedAtUtc = DateTime.UtcNow;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task VerifyEnrollmentFromPartnerAsync(
        string partnerExternalId,
        string candidateHash,
        string certificateTitle,
        CancellationToken cancellationToken = default)
    {
        var click = await _db.TrainingClicks.AsNoTracking()
            .Where(c => c.CandidateHash == candidateHash)
            .OrderByDescending(c => c.ClickedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Click not found.");

        var offer = await _db.TrainingOffers
            .FirstOrDefaultAsync(
                o => o.PartnerExternalId == partnerExternalId && o.Id == click.OfferId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Offer not found.");

        if (click.UserId is not Guid userId)
        {
            throw new InvalidOperationException("Anonymous click cannot be verified.");
        }

        var row = await _db.Set<TrainingEnrollment>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.OfferId == offer.Id, cancellationToken);
        if (row is null)
        {
            row = new TrainingEnrollment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OfferId = offer.Id
            };
            _db.Set<TrainingEnrollment>().Add(row);
        }

        row.Status = TrainingEnrollmentStatus.Verified;
        row.CertificateTitle = certificateTitle.Trim();
        row.VerifiedAtUtc = DateTime.UtcNow;
        row.VerificationSource = TrainingCertificateVerificationSource.PartnerWebhook;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await AppendCertificateAsync(userId, certificateTitle, cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingPartnerAdminDto>> ListPartnersAdminAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.TrainingProviders.AsNoTracking()
            .Select(p => new TrainingPartnerAdminDto(
                p.Id,
                p.Name,
                p.Kind,
                p.FeedKind,
                p.FeedUrl,
                p.FeedMappingJson,
                p.FeedApiSecretName,
                p.ImportIntervalHours,
                p.LastImportedAtUtc,
                p.IsActive,
                p.IsDemoPartner,
                p.Offers.Count))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<TrainingPartnerAdminDto> UpsertPartnerAdminAsync(
        TrainingPartnerAdminUpsert request,
        CancellationToken cancellationToken = default)
    {
        TrainingProvider row;
        if (request.Id is Guid id)
        {
            row = await _db.TrainingProviders.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                  ?? throw new KeyNotFoundException("Partner not found.");
        }
        else
        {
            row = new TrainingProvider { Id = Guid.NewGuid() };
            _db.TrainingProviders.Add(row);
        }

        row.Name = request.Name.Trim();
        row.Kind = request.Kind;
        row.FeedKind = request.FeedKind;
        row.FeedUrl = request.FeedUrl?.Trim();
        row.FeedMappingJson = request.FeedMappingJson;
        row.FeedApiSecretName = request.FeedApiSecretName?.Trim();
        row.ImportIntervalHours = Math.Clamp(request.ImportIntervalHours, 1, 168);
        row.IsActive = request.IsActive;
        row.IsDemoPartner = request.IsDemoPartner;
        row.Network = TrainingNetwork.Direct;
        row.BaseUrl = row.BaseUrl.Length > 0 ? row.BaseUrl : "https://partner.lobsy.nl/";
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (await ListPartnersAdminAsync(cancellationToken)).First(p => p.Id == row.Id);
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

    private async Task<List<TrainingOffer>> ActiveOffersAsync(CancellationToken ct)
        => await _db.TrainingOffers.AsNoTracking()
            .Include(o => o.Provider)
            .Where(o => o.IsActive && o.Provider.IsActive)
            .Where(o => !o.Provider.IsDemoPartner || AllowDemoSeed())
            .ToListAsync(ct);

    private static IEnumerable<TrainingOffer> ApplyFilters(IEnumerable<TrainingOffer> rows, TrainingLearnGrowQuery query)
    {
        IEnumerable<TrainingOffer> q = rows;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(o => o.Title.Contains(s, StringComparison.OrdinalIgnoreCase)
                             || o.KeysCsv.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Subject) && !query.Subject.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(o => TrainingMatchRules.SplitCsv(o.CompetenciesCsv).Contains(query.Subject!, StringComparer.OrdinalIgnoreCase)
                             || TrainingMatchRules.SplitCsv(o.KeysCsv).Contains(query.Subject!, StringComparer.OrdinalIgnoreCase));
        }

        if (query.FreeOnly)
        {
            q = q.Where(o => o.IsFree);
        }

        if (!string.IsNullOrWhiteSpace(query.Format))
        {
            q = query.Format.ToLowerInvariant() switch
            {
                "online" => q.Where(o => o.Delivery == TrainingDeliveryMode.Online),
                "klassikaal" => q.Where(o => o.Delivery is TrainingDeliveryMode.OnSite or TrainingDeliveryMode.Blended),
                _ => q
            };
        }

        if (!string.IsNullOrWhiteSpace(query.Duration))
        {
            q = query.Duration.ToLowerInvariant() switch
            {
                "kort" => q.Where(o => o.DurationUnit is TrainingDurationUnit.Hours or TrainingDurationUnit.Days),
                "weken" => q.Where(o => o.DurationUnit == TrainingDurationUnit.Weeks),
                "opleiding" => q.Where(o => o.Type == TrainingOfferType.Opleiding),
                _ => q
            };
        }

        return q;
    }

    private static int ScoreOffer(TrainingOffer offer, IReadOnlyList<string> fields, string blob, string? stepTitle)
    {
        var stepFold = CareerCourseMatcher.Normalize(stepTitle);
        var stepHit = TrainingMatchRules.SplitCsv(offer.CareerStepsCsv)
            .Any(s => stepFold.Contains(CareerCourseMatcher.Normalize(s), StringComparison.Ordinal))
            || offer.Section == TrainingOfferSection.NextStep;
        var baseScore = TrainingMatchRules.Score(
            offer.Provider.Kind,
            TrainingMatchRules.SplitCsv(offer.FieldsCsv),
            TrainingMatchRules.SplitCsv(offer.KeysCsv),
            fields,
            blob);
        return baseScore + (stepHit ? 25 : 0) + (int)offer.Section;
    }

    private async Task<Dictionary<Guid, TrainingEnrollment>> LoadEnrollmentMapAsync(Guid userId, CancellationToken ct)
        => await _db.Set<TrainingEnrollment>()
            .Where(e => e.UserId == userId)
            .ToDictionaryAsync(e => e.OfferId, ct);

    private static TrainingPartnerOfferDto ToDto(
        TrainingOffer offer,
        IReadOnlyDictionary<Guid, TrainingEnrollment> enrollments,
        bool english,
        string? _)
    {
        enrollments.TryGetValue(offer.Id, out var enrollment);
        var bullets = ParseBullets(offer.LearnBulletsJson);
        var reason = CareerCourseMatcher.FormatRelevanceReason(
            english,
            offer.ReasonNl,
            offer.ReasonEn,
            "Past bij jouw route.",
            "Fits your route.");

        return new TrainingPartnerOfferDto(
            offer.Id,
            offer.Title,
            offer.Provider.Name,
            offer.Type.ToString(),
            offer.DurationValue,
            offer.DurationUnit?.ToString(),
            offer.Delivery.ToString(),
            offer.Location,
            offer.IsFree,
            offer.PriceEuro,
            offer.PriceLabel,
            reason,
            enrollment?.Status == TrainingEnrollmentStatus.Saved,
            offer.Section,
            bullets,
            offer.StartsOn);
    }

    private static IReadOnlyList<string> ParseBullets(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<bool> IsEnglishAsync(Guid userId, CancellationToken ct)
    {
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var prefs = JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions);
            return string.Equals(prefs?.Language, "en", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<string> LoadDreamTitleAsync(Guid userId, CancellationToken ct)
    {
        var title = await _db.CandidateCareerPlans.AsNoTracking()
            .Where(p => p.UserId == userId && p.ArchivedAtUtc == null)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Select(p => p.DreamTitle)
            .FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(title) ? "jouw droombaan" : title;
    }

    private async Task<CompetencyScoresSnapshot?> LoadCompetencyScoresAsync(Guid userId, CancellationToken ct)
    {
        var row = await _db.CandidateCompetencies.AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == CandidateCompetencyStatuses.Completed)
            .OrderByDescending(s => s.CompletedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (row is null)
        {
            return null;
        }

        return new CompetencyScoresSnapshot(
            row.SamenwerkenPercent,
            row.ResultaatgerichtheidPercent,
            row.StressbestendigheidPercent,
            row.InnovatiePercent,
            row.ExtraversiePercent);
    }

    private static int? ScoreForCategory(CompetencyScoresSnapshot scores, string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => scores.Samenwerken,
        CompetencyTestCatalog.Resultaatgerichtheid => scores.Resultaatgerichtheid,
        CompetencyTestCatalog.Stressbestendigheid => scores.Stressbestendigheid,
        CompetencyTestCatalog.Innovatie => scores.Innovatie,
        CompetencyTestCatalog.Extraversie => scores.Extraversie,
        _ => null
    };

    private static string GrowthSubtitle(CompetencyScoresSnapshot? scores)
    {
        if (scores is null)
        {
            return "";
        }

        var low = new List<string>();
        if (scores.Stressbestendigheid is < CompetencySkillBands.GrowingMin)
        {
            low.Add("rust onder druk");
        }

        if (scores.Resultaatgerichtheid is < CompetencySkillBands.GrowingMin)
        {
            low.Add("plannen");
        }

        return low.Count == 0 ? "" : string.Join(" en ", low);
    }

    private async Task AppendCertificateAsync(Guid userId, string title, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new InvalidOperationException("User not found.");
        CandidatePreferencesDto prefs;
        try
        {
            prefs = string.IsNullOrWhiteSpace(user.PreferencesJson)
                ? new CandidatePreferencesDto([], null, null)
                : JsonSerializer.Deserialize<CandidatePreferencesDto>(user.PreferencesJson, JsonOptions)
                  ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            prefs = new CandidatePreferencesDto([], null, null);
        }

        var certs = prefs.Certificates?.ToList() ?? [];
        if (certs.Any(c => CareerCourseMatcher.IsMatch(title, c.Name)))
        {
            return;
        }

        certs.Add(new CandidateCertificateDto(title.Trim(), DateTime.UtcNow.Year));
        prefs = prefs with { Certificates = certs };
        user.PreferencesJson = JsonSerializer.Serialize(prefs, JsonOptions);
        await _db.SaveChangesAsync(ct);
    }

    private sealed record CompetencyScoresSnapshot(
        int? Samenwerken,
        int? Resultaatgerichtheid,
        int? Stressbestendigheid,
        int? Innovatie,
        int? Extraversie);
}
