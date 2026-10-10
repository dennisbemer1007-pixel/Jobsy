using System.Text.Json;
using Jobsy.Core;
using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class CandidateExternalVacancyService : ICandidateExternalVacancyService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JobsyDbContext _db;
    private readonly IExternalVacancyUrlFetchService _fetch;
    private readonly IExternalVacancyExtractionService _extraction;
    private readonly IExternalVacancyMatchService _match;
    private readonly IExternalVacancyContactFinder _contacts;
    private readonly IExternalVacancyApplicationLetterPdfBuilder _pdf;
    private readonly IExternalVacancySuppressionService _suppression;
    private readonly IOneTimeLinkService _oneTimeLinks;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<CandidateExternalVacancyService> _logger;

    public CandidateExternalVacancyService(
        JobsyDbContext db,
        IExternalVacancyUrlFetchService fetch,
        IExternalVacancyExtractionService extraction,
        IExternalVacancyMatchService match,
        IExternalVacancyContactFinder contacts,
        IExternalVacancyApplicationLetterPdfBuilder pdf,
        IExternalVacancySuppressionService suppression,
        IOneTimeLinkService oneTimeLinks,
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        ILogger<CandidateExternalVacancyService> logger)
    {
        _db = db;
        _fetch = fetch;
        _extraction = extraction;
        _match = match;
        _contacts = contacts;
        _pdf = pdf;
        _suppression = suppression;
        _oneTimeLinks = oneTimeLinks;
        _mailer = mailer;
        _features = features;
        _logger = logger;
    }

    public async Task<ExternalVacancyDetailDto> ImportAsync(
        Guid candidateUserId,
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!CandidateExternalVacancyRules.TryNormalizeSourceUrl(url, out var uri, out var urlError))
        {
            throw new ArgumentException(urlError);
        }

        await EnsureImportRateLimitAsync(candidateUserId, cancellationToken);

        var fetched = await _fetch.FetchAsync(uri, cancellationToken)
                      ?? throw new InvalidOperationException("fetch_failed");
        var extracted = await _extraction.ExtractAsync(fetched.VisibleText, cancellationToken)
                        ?? new ExternalVacancyExtractionResult("", "", "", "", "", "", "", []);

        var structured = BuildStructuredFacts(extracted);
        var insights = await _match.BuildMatchInsightsAsync(candidateUserId, structured, cancellationToken);
        var suggested = await _contacts.FindEmployerEmailsAsync(uri, extracted.Company, cancellationToken);

        var row = new CandidateExternalVacancy
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateUserId,
            SourceUrl = uri.ToString(),
            SourceHost = uri.Host,
            Title = extracted.Title,
            CompanyName = extracted.Company,
            Place = extracted.Place,
            HoursText = extracted.Hours,
            PayText = extracted.Pay,
            StartText = extracted.Start,
            TrainingText = extracted.Training,
            RequirementsBulletsJson = JsonSerializer.Serialize(extracted.RequirementBullets, JsonOptions),
            StructuredFactsJson = JsonSerializer.Serialize(structured, JsonOptions),
            MatchInsightsJson = JsonSerializer.Serialize(insights, JsonOptions),
            SavedAtUtc = DateTime.UtcNow,
            Status = CandidateExternalVacancyStatus.Saved
        };

        _db.CandidateExternalVacancies.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapDetailAsync(row, suggested, cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalVacancyListItemDto>> ListAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.CandidateExternalVacancies.AsNoTracking()
            .Where(v => v.CandidateUserId == candidateUserId)
            .OrderByDescending(v => v.SavedAtUtc)
            .Select(v => new ExternalVacancyListItemDto(
                v.Id,
                v.Title,
                v.CompanyName,
                v.Place,
                v.Status,
                v.SavedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExternalVacancyDetailDto?> GetDetailAsync(
        Guid candidateUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateExternalVacancies.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && v.CandidateUserId == candidateUserId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var uri = Uri.TryCreate(row.SourceUrl, UriKind.Absolute, out var u) ? u : null;
        var suggested = uri is null
            ? Array.Empty<string>()
            : await _contacts.FindEmployerEmailsAsync(uri, row.CompanyName, cancellationToken);
        return await MapDetailAsync(row, suggested, cancellationToken);
    }

    public async Task<ExternalVacancyApplyResultDto> ApplyAsync(
        Guid candidateUserId,
        Guid id,
        ExternalVacancyApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CandidateExternalVacancyRules.IsValidEmployerEmail(request.EmployerEmail))
        {
            return new(false, "invalid_email", "Ongeldig e-mailadres.");
        }

        var motivationError = CandidateExternalVacancyRules.ValidateMotivation(request.Motivation);
        if (motivationError is not null)
        {
            return new(false, "invalid_motivation", motivationError);
        }

        var normalizedEmail = CandidateExternalVacancyRules.NormalizeEmployerEmail(request.EmployerEmail);
        if (await _suppression.IsSuppressedAsync(normalizedEmail, cancellationToken))
        {
            return new(false, "suppressed", "Dit adres accepteert geen sollicitaties via Lobsy.");
        }

        if (!await EnsureApplyRateLimitAsync(candidateUserId, cancellationToken))
        {
            return new(false, "rate_limit", "Je hebt vandaag het maximum aan sollicitaties via Lobsy bereikt.");
        }

        var vacancy = await _db.CandidateExternalVacancies
            .Include(v => v.CandidateUser)
            .Include(v => v.OutboundMessages)
            .FirstOrDefaultAsync(v => v.Id == id && v.CandidateUserId == candidateUserId, cancellationToken);
        if (vacancy is null)
        {
            return new(false, "not_found", null);
        }

        if (vacancy.OutboundMessages.Any(o => o.EmployerEmailNormalized == normalizedEmail))
        {
            return new(false, "already_sent", "Je hebt al naar dit adres gestuurd voor deze vacature.");
        }

        var structured = DeserializeDictionary(vacancy.StructuredFactsJson);
        var keys = CandidateExternalVacancyRules.FilterSharedFactKeys(request.SharedFactKeys, structured);
        var shared = keys.ToDictionary(k => k, k => structured[k], StringComparer.OrdinalIgnoreCase);

        var link = await _oneTimeLinks.CreateAsync(
            OneTimeLinkPurpose.ExternalVacancyEmployerInvite,
            userId: null,
            companyId: null,
            normalizedEmail,
            OneTimeLinkRules.ExternalVacancyEmployerInviteLifetime,
            candidateUserId,
            cancellationToken);

        var outbound = new CandidateExternalVacancyOutbound
        {
            Id = Guid.NewGuid(),
            ExternalVacancyId = vacancy.Id,
            EmployerEmailNormalized = normalizedEmail,
            Motivation = request.Motivation.Trim(),
            SharedFactsJson = JsonSerializer.Serialize(shared, JsonOptions),
            InitialSentAtUtc = DateTime.UtcNow,
            OneTimeLinkId = link.Id
        };
        _db.CandidateExternalVacancyOutbounds.Add(outbound);
        vacancy.Status = CandidateExternalVacancyStatus.Applied;
        await _db.SaveChangesAsync(cancellationToken);

        var platform = await _features.GetAsync(cancellationToken);
        var inviteUrl = $"{platform.PublicWebBaseUrl.TrimEnd('/')}/register/externe-sollicitatie?token={Uri.EscapeDataString(link.Token)}";
        var unsubToken = _suppression.CreateUnsubscribeToken(normalizedEmail);
        var apiBase = JobsyPublicUrl.NormalizeBaseUrl(platform.PublicWebBaseUrl, "http://localhost:5200/");
        var unsubUrl =
            $"{apiBase}api/public/external-vacancy/unsubscribe?token={Uri.EscapeDataString(unsubToken)}";

        var mail = TransactionalEmails.ExternalVacancyApplication(
            platform.PublicWebBaseUrl,
            vacancy.CandidateUser.FullName,
            vacancy.Title,
            vacancy.CompanyName,
            request.Motivation.Trim(),
            shared.Select(kv => (kv.Key, kv.Value)).ToList(),
            inviteUrl,
            unsubUrl);

        var send = await _mailer.SendAsync(mail, normalizedEmail, cancellationToken: cancellationToken);
        if (!send.Sent)
        {
            _logger.LogWarning("External vacancy mail not sent to employer (suppressed={Suppressed})", send.Suppressed);
        }

        return new(true, null, null);
    }

    public async Task<byte[]?> BuildApplicationLetterPdfAsync(
        Guid candidateUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vacancy = await _db.CandidateExternalVacancies.AsNoTracking()
            .Include(v => v.CandidateUser)
            .FirstOrDefaultAsync(v => v.Id == id && v.CandidateUserId == candidateUserId, cancellationToken);
        if (vacancy is null)
        {
            return null;
        }

        var structured = DeserializeDictionary(vacancy.StructuredFactsJson);
        var facts = structured.Select(kv => (kv.Key, kv.Value)).ToList();
        var outbound = await _db.CandidateExternalVacancyOutbounds.AsNoTracking()
            .Where(o => o.ExternalVacancyId == id)
            .OrderByDescending(o => o.InitialSentAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var motivation = outbound?.Motivation ?? "";
        return _pdf.Build(
            vacancy.CandidateUser.FullName,
            vacancy.Title,
            vacancy.CompanyName,
            motivation,
            facts);
    }

    public async Task<ExternalVacancyAdminMetricsDto> GetAdminMetricsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.CandidateExternalVacancyOutbounds.AsNoTracking().ToListAsync(cancellationToken);
        return new ExternalVacancyAdminMetricsDto(
            rows.Count,
            rows.Count(r => r.ReminderSentAtUtc is not null),
            rows.Count(r => r.OpenedAtUtc is not null),
            rows.Count(r => r.ClickedAtUtc is not null),
            rows.Count(r => r.EmployerAccountCreatedAtUtc is not null),
            rows.Count(r => r.AcceptedAtUtc is not null));
    }

    private async Task EnsureImportRateLimitAsync(Guid candidateUserId, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date;
        var count = await _db.CandidateExternalVacancies.AsNoTracking()
            .CountAsync(v => v.CandidateUserId == candidateUserId && v.SavedAtUtc >= since, cancellationToken);
        if (count >= CandidateExternalVacancyRules.MaxImportsPerUserPerDay)
        {
            throw new InvalidOperationException("rate_limit");
        }
    }

    private async Task<bool> EnsureApplyRateLimitAsync(Guid candidateUserId, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date;
        var count = await _db.CandidateExternalVacancyOutbounds.AsNoTracking()
            .CountAsync(
                o => o.InitialSentAtUtc >= since
                     && _db.CandidateExternalVacancies.Any(v =>
                         v.Id == o.ExternalVacancyId && v.CandidateUserId == candidateUserId),
                cancellationToken);
        return count < CandidateExternalVacancyRules.MaxAppliesPerUserPerDay;
    }

    private async Task<ExternalVacancyDetailDto> MapDetailAsync(
        CandidateExternalVacancy row,
        IReadOnlyList<string> suggestedEmails,
        CancellationToken cancellationToken)
    {
        var hasOutbound = await _db.CandidateExternalVacancyOutbounds.AsNoTracking()
            .AnyAsync(o => o.ExternalVacancyId == row.Id, cancellationToken);
        var bullets = JsonSerializer.Deserialize<List<string>>(row.RequirementsBulletsJson, JsonOptions) ?? [];
        var structured = DeserializeDictionary(row.StructuredFactsJson);
        var insights = JsonSerializer.Deserialize<ExternalVacancyMatchInsightsDto>(row.MatchInsightsJson, JsonOptions)
                       ?? new ExternalVacancyMatchInsightsDto([], []);
        return new ExternalVacancyDetailDto(
            row.Id,
            row.SourceUrl,
            row.Title,
            row.CompanyName,
            row.Place,
            row.HoursText,
            row.PayText,
            row.StartText,
            row.TrainingText,
            bullets,
            structured,
            insights,
            row.TravelMinutesEstimate,
            row.Status,
            row.SavedAtUtc,
            !hasOutbound,
            suggestedEmails);
    }

    private static Dictionary<string, string> BuildStructuredFacts(ExternalVacancyExtractionResult extracted)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Add(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                dict[key] = value.Trim();
            }
        }

        Add("bedrijf", extracted.Company);
        Add("plaats", extracted.Place);
        Add("uren", extracted.Hours);
        Add("salaris", extracted.Pay);
        Add("start", extracted.Start);
        Add("opleiding", extracted.Training);
        return dict;
    }

    private static Dictionary<string, string> DeserializeDictionary(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
