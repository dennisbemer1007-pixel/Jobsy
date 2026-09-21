using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts;
using Jobsy.Core;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Jobsy.Infrastructure.Data;

namespace Jobsy.Infrastructure.Services;

public sealed class RoleFitCheckService : IRoleFitCheckService
{
    public const string HttpClientName = "OpenAIRoleFit";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly ICandidateCompetencyService _competencies;
    private readonly ICandidateCareerInterestService _career;
    private readonly IFlexCommercialService _commercial;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<RoleFitCheckService> _logger;
    private readonly ITrainingUpskillService _training;
    private readonly ICultureFitAiService _cultureFit;
    private readonly IVacancyDiscoveryIndex _discovery;
    private readonly IProfileVacancyMatchService _matches;

    public RoleFitCheckService(
        JobsyDbContext db,
        ICandidateCompetencyService competencies,
        ICandidateCareerInterestService career,
        IFlexCommercialService commercial,
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<RoleFitCheckService> logger,
        ITrainingUpskillService training,
        ICultureFitAiService cultureFit,
        IVacancyDiscoveryIndex discovery,
        IProfileVacancyMatchService matches)
    {
        _db = db;
        _competencies = competencies;
        _career = career;
        _commercial = commercial;
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
        _training = training;
        _cultureFit = cultureFit;
        _discovery = discovery;
        _matches = matches;
    }

    public async Task<RoleFitCheckStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var competence = await _competencies.GetCompletedScoresAsync(userId, cancellationToken);
        var career = await _career.GetCompletedScoresAsync(userId, cancellationToken);
        var unlocked = competence is { IsComplete: true } && career is { IsComplete: true };
        var deep = await HasCompletedDeepAsync(userId, cancellationToken);
        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
        var last = await _db.CandidateRoleFitChecks.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        return new RoleFitCheckStateDto(
            unlocked,
            competence is { IsComplete: true },
            career is { IsComplete: true },
            deep,
            price,
            unlocked ? "" : RoleFitCheckCopy.Locked,
            RoleFitCheckCopy.DeepUpsell,
            last is null ? null : await ToResultAsync(userId, last, deep, cancellationToken));
    }

    public async Task<RoleFitCheckStateDto> EvaluateAsync(
        Guid userId,
        string? jobTitle,
        Guid? vacancyId = null,
        CancellationToken cancellationToken = default)
    {
        Vacancy? vacancy = null;
        if (vacancyId is Guid vid)
        {
            vacancy = await _db.Vacancies.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vid && v.Status == VacancyStatus.Active, cancellationToken);
            if (vacancy is null)
            {
                throw new InvalidOperationException("Vacature niet gevonden.");
            }
        }

        var title = vacancy is not null
            ? RoleFitCheckBuilder.NormalizeTitle(vacancy.Title)
              ?? RoleFitCheckBuilder.NormalizeTitle(jobTitle)
            : RoleFitCheckBuilder.NormalizeTitle(jobTitle);
        if (title is null)
        {
            throw new InvalidOperationException(RoleFitCheckCopy.TitleEmpty);
        }

        var competence = await _competencies.GetCompletedScoresAsync(userId, cancellationToken);
        var career = await _career.GetCompletedScoresAsync(userId, cancellationToken);
        if (competence is not { IsComplete: true } || career is not { IsComplete: true })
        {
            throw new RoleFitLockedException();
        }

        var fromDeep = await HasCompletedDeepAsync(userId, cancellationToken);
        var local = RoleFitCheckBuilder.Build(title, competence, career, fromDeep);
        var snapshot = await TryOpenAiAsync(title, competence, career, fromDeep, userId, local, cancellationToken)
                       ?? local;
        snapshot = snapshot with
        {
            SimilarRoles = RoleFitFunnel.MergeSimilar(snapshot.SimilarRoles, local.SimilarRoles)
        };
        if (vacancy is not null)
        {
            snapshot = await AttachVacancyAsync(userId, vacancy, competence, snapshot, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var row = await _db.CandidateRoleFitChecks.FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new CandidateRoleFitCheck
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            _db.CandidateRoleFitChecks.Add(row);
        }

        row.JobTitle = snapshot.JobTitle;
        row.MatchPercent = snapshot.MatchPercent;
        row.ResultJson = RoleFitCheckJson.Serialize(snapshot);
        row.FromDeepAnalysis = snapshot.FromDeepAnalysis;
        row.FromOpenAi = snapshot.FromOpenAi;
        row.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);

        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
        return new RoleFitCheckStateDto(
            true,
            true,
            true,
            fromDeep,
            price,
            "",
            RoleFitCheckCopy.DeepUpsell,
            await ToResultAsync(userId, row, fromDeep, cancellationToken));
    }

    private async Task<RoleFitCheckSnapshot?> TryOpenAiAsync(
        string jobTitle,
        CompetencyScores competencies,
        RiasecScores career,
        bool fromDeep,
        Guid userId,
        RoleFitCheckSnapshot fallback,
        CancellationToken cancellationToken)
    {
        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            var prefs = await LoadPracticalCriteriaAsync(userId, cancellationToken);
            var user = RoleFitCheckPrompt.User(
                jobTitle,
                competencies,
                career,
                fromDeep,
                prefs.MaxTravelMinutes,
                prefs.Transport,
                prefs.Licenses,
                prefs.Roles);
            var model = await ResolveModelAsync(cancellationToken);
            var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(new
            {
                model,
                temperature = 0.3,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = RoleFitCheckPrompt.System },
                    new { role = "user", content = user }
                }
            });

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI functie-fit gaf {StatusCode} (response body not logged).",
                    (int)response.StatusCode);
                return null;
            }

            var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            var parsed = RoleFitCheckJson.TryDeserialize(content, jobTitle, fromDeep);
            if (parsed is null || parsed.Strengths.Count == 0)
            {
                return null;
            }

            return parsed with { FromOpenAi = true, FromDeepAnalysis = fromDeep };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI functie-fit mislukt; lokale indicatie wordt gebruikt.");
            return fallback with { FromOpenAi = false };
        }
    }

    private async Task<(int? MaxTravelMinutes, string? Transport, IReadOnlyList<string>? Licenses, IReadOnlyList<string>? Roles)>
        LoadPracticalCriteriaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return (null, null, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int? minutes = root.TryGetProperty("maxTravelMinutes", out var m) && m.ValueKind == JsonValueKind.Number
                ? m.GetInt32()
                : null;
            var transport = root.TryGetProperty("preferredTransport", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString()
                : null;
            List<string>? licenses = ReadStringArray(root, "drivingLicenses");
            List<string>? roles = ReadStringArray(root, "roles");
            return (minutes, transport, licenses, roles);
        }
        catch (JsonException)
        {
            return (null, null, null, null);
        }
    }

    private static List<string>? ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<string>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value) && !value.Contains('@', StringComparison.Ordinal))
                {
                    list.Add(value.Trim());
                }
            }
        }

        return list.Count == 0 ? null : list;
    }

    private Task<bool> HasCompletedDeepAsync(Guid userId, CancellationToken cancellationToken)
        => _db.CandidateDeepAnalyses.AsNoTracking()
            .AnyAsync(
                d => d.UserId == userId && d.Status == CandidateDeepAnalysisStatuses.Completed,
                cancellationToken);

    private async Task<RoleFitCheckResultDto> ToResultAsync(
        Guid userId,
        CandidateRoleFitCheck row,
        bool deepNow,
        CancellationToken cancellationToken)
    {
        var snapshot = RoleFitCheckJson.TryDeserialize(row.ResultJson, row.JobTitle, row.FromDeepAnalysis || deepNow)
                       ?? new RoleFitCheckSnapshot(
                           row.JobTitle,
                           row.MatchPercent,
                           [],
                           [],
                           [],
                           CareerOccupationKeys.FromTitle(row.JobTitle),
                           row.FromDeepAnalysis || deepNow,
                           row.FromOpenAi);
        var query = Uri.EscapeDataString(snapshot.MapQuery);
        var fit = snapshot.VacancyFit;
        var searchKeys = snapshot.SearchKeys;
        if (fit is { ShowUpskill: true, FormalItems: { Count: > 0 } })
        {
            searchKeys = fit.FormalItems.Where(i => !i.Met).Select(i => i.Label).Concat(searchKeys).ToList();
        }

        var offers = (fit is { ShowUpskill: true } || snapshot.Gaps.Count > 0)
            ? await _training.RecommendAsync(
                userId,
                snapshot.JobTitle,
                searchKeys,
                TrainingTracking.CampaignFit,
                cancellationToken)
            : Array.Empty<TrainingOfferCardDto>();
        if (fit is { ShowUpskill: false })
        {
            offers = [];
        }

        var similar = (snapshot.SimilarRoles ?? [])
            .Select(s => new RoleFitSimilarRoleDto(s.Title, s.Why, s.FitPercent))
            .ToList();
        var direct = await ScanDirectVacanciesAsync(userId, fit?.VacancyId, cancellationToken);

        return new RoleFitCheckResultDto(
            snapshot.JobTitle,
            snapshot.MatchPercent,
            snapshot.Strengths,
            snapshot.Gaps,
            snapshot.ActionSteps,
            snapshot.SearchKeys,
            $"/?q={query}",
            snapshot.FromDeepAnalysis,
            snapshot.FromOpenAi,
            snapshot.ShowDeepUpsell,
            offers,
            fit?.VacancyId,
            fit?.BarrierKind,
            fit?.CulturePercent,
            fit?.CultureBand,
            fit?.CultureLabel,
            fit?.CultureWhy,
            fit?.FormalItems.Select(i => new RoleFitFormalItemDto(i.Key, i.Label, i.Met, i.Note)).ToList(),
            fit?.ShowFormalBlock ?? false,
            fit?.ShowUpskill ?? false,
            fit?.AvailabilityOk ?? true,
            similar,
            direct);
    }

    private async Task<RoleFitCheckSnapshot> AttachVacancyAsync(
        Guid userId,
        Vacancy vacancy,
        CompetencyScores competence,
        RoleFitCheckSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var prefs = MatchingProfileMapper.DeserializePrefs(user?.PreferencesJson);
        var requirements = VacancyBarrierCatalog.Deserialize(vacancy.BarrierRequirementsJson);
        var formal = VacancyBarrierCatalog.Evaluate(requirements, prefs);
        var availability = VacancyBarrierCatalog.AvailabilityLooksOk(
            prefs,
            vacancy.MinHoursPerWeek,
            vacancy.MaxHoursPerWeek,
            vacancy.RequiredDrivingLicense,
            vacancy.RequiredEducation);
        var pillars = CulturePillarCatalog.Deserialize(vacancy.CulturePillarsJson);
        var culture = CultureFitBuilder.Evaluate(pillars, competence);
        if (culture is not null)
        {
            var labels = pillars
                .Select(id => CulturePillarCatalog.TryGet(id, out var d) ? d.Label : id)
                .ToList();
            culture = await _cultureFit.TryRefineAsync(culture, competence, labels, cancellationToken) ?? culture;
        }

        var vacancyFit = RoleFitCheckBuilder.BuildVacancyFit(vacancy.Id, requirements, formal, culture, availability);
        var overall = RoleFitFunnel.CombineOverall(snapshot.MatchPercent, availability, culture?.Percent, formal);
        var extraStrengths = new List<string>();
        if (availability)
        {
            extraStrengths.Add("Reistijd, uren en basisbeschikbaarheid passen bij deze vacature.");
        }

        if (culture is { Percent: >= CultureFitBuilder.MidThreshold } && !string.IsNullOrWhiteSpace(culture.Why))
        {
            extraStrengths.Add(culture.Why);
        }

        extraStrengths.AddRange(formal.Items.Where(i => i.Met).Select(i => i.Note));
        extraStrengths.AddRange(snapshot.Strengths);

        var extraGaps = new List<string>();
        if (!availability)
        {
            extraGaps.Add("Reistijd, uren of rijbewijs/opleidingsniveau klopt nog niet met deze vacature.");
        }

        extraGaps.AddRange(formal.Items.Where(i => !i.Met).Select(i => i.Note));
        extraGaps.AddRange(snapshot.Gaps);

        if (vacancyFit.ShowUpskill)
        {
            snapshot = snapshot with
            {
                ActionSteps = snapshot.ActionSteps
                    .Prepend(TrainingCopy.GapAdvice)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList()
            };
        }

        return RoleFitCheckBuilder.Sanitize(snapshot with
        {
            MatchPercent = overall,
            Strengths = extraStrengths.Take(5).ToList(),
            Gaps = extraGaps.Count > 0 ? extraGaps.Take(5).ToList() : snapshot.Gaps,
            VacancyFit = vacancyFit
        });
    }

    private async Task<IReadOnlyList<RoleFitDirectVacancyDto>> ScanDirectVacanciesAsync(
        Guid userId,
        Guid? excludeVacancyId,
        CancellationToken cancellationToken)
    {
        var context = await _matches.TryLoadForUserIdAsync(userId, cancellationToken);
        if (context is null)
        {
            return [];
        }

        var vacancies = await _discovery.GetActiveAsync(cancellationToken);
        var transport = TransportLabels.Parse(context.Prefs.PreferredTransport);
        var scored = _matches.Score(
            context,
            vacancies.Select(vacancy =>
            {
                int? travelMinutes = null;
                if (context.HomeLatitude is double lat && context.HomeLongitude is double lng)
                {
                    var estimate = TravelReach.Estimate(
                        lat,
                        lng,
                        vacancy.Latitude,
                        vacancy.Longitude,
                        transport);
                    travelMinutes = estimate.TravelMinutes;
                }

                return (vacancy, travelMinutes);
            }));

        var ranked = scored.Values
            .Where(m => m.VacancyId != excludeVacancyId)
            .Where(m => m.Core.LegalEligible && m.TotalPercent >= RoleFitFunnel.DirectMatchMin)
            .OrderByDescending(m => m.TotalPercent)
            .Take(40)
            .ToList();
        if (ranked.Count == 0)
        {
            return [];
        }

        var ids = ranked.Select(m => m.VacancyId).ToList();
        var barriers = await _db.Vacancies.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Select(v => new { v.Id, v.BarrierRequirementsJson })
            .ToDictionaryAsync(v => v.Id, cancellationToken);
        var byId = vacancies.ToDictionary(v => v.Id);
        var cards = new List<RoleFitDirectVacancyDto>();
        foreach (var match in ranked)
        {
            if (!byId.TryGetValue(match.VacancyId, out var record))
            {
                continue;
            }

            barriers.TryGetValue(match.VacancyId, out var row);
            var requirements = VacancyBarrierCatalog.Deserialize(row?.BarrierRequirementsJson);
            var formal = VacancyBarrierCatalog.Evaluate(requirements, context.Prefs);
            var availability = VacancyBarrierCatalog.AvailabilityLooksOk(
                context.Prefs,
                record.MinHoursPerWeek,
                record.MaxHoursPerWeek,
                record.RequiredDrivingLicense,
                record.RequiredEducation);
            if (!RoleFitFunnel.CanStartImmediately(
                    match.Core.LegalEligible,
                    match.TotalPercent,
                    match.Core.TravelWithinPreference,
                    formal,
                    availability))
            {
                continue;
            }

            cards.Add(new RoleFitDirectVacancyDto(
                record.Id,
                record.Title,
                record.CompanyName,
                match.TotalPercent,
                "/vacancies/" + record.Id.ToString("D")));
            if (cards.Count >= RoleFitFunnel.MaxDirect)
            {
                break;
            }
        }

        return cards;
    }

    private async Task<string?> ResolveApiKeyAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetRawApiKeyAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey.Trim();
    }

    private async Task<string> ResolveModelAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetModelAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model.Trim();
    }

    private async Task<string> ResolveBaseUrlAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetBaseUrlAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb)
            && IntegrationEndpointUrl.TryNormalizeBaseUrl(fromDb, out var normalized, out _)
            && !string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var fallback = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1/"
            : _options.BaseUrl;
        if (IntegrationEndpointUrl.TryNormalizeBaseUrl(fallback, out var normalizedFallback, out _)
            && !string.IsNullOrWhiteSpace(normalizedFallback))
        {
            return normalizedFallback;
        }

        return "https://api.openai.com/v1/";
    }

    private sealed class ChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }

        public sealed class Choice
        {
            public Message? Message { get; set; }
        }

        public sealed class Message
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
